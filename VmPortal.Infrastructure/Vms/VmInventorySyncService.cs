using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VmPortal.Application.Proxmox;
using VmPortal.Application.Vms;
using VmPortal.Domain.Vms;
using VmPortal.Infrastructure.Data;
using System.Diagnostics;

namespace VmPortal.Infrastructure.Vms
{
    internal sealed class VmInventorySyncService : IVmInventorySyncService
    {
        private readonly VmPortalDbContext _db;
        private readonly IProxmoxClient _proxmox;
        private readonly ILogger<VmInventorySyncService> _logger;

        public VmInventorySyncService(
            VmPortalDbContext db,
            IProxmoxClient proxmox,
            ILogger<VmInventorySyncService> logger)
        {
            _db = db;
            _proxmox = proxmox;
            _logger = logger;
        }

        public async Task SyncAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting VM inventory sync...");

            Stopwatch stopwatch = Stopwatch.StartNew();

            List<Vm> dbVms = await _db.Vms
                .Where(v => !v.IsDeleted)
                .ToListAsync(cancellationToken);

            IReadOnlyList<ProxmoxVmInfo> proxmoxVms = await _proxmox.ListVmsAsync(cancellationToken);

            Dictionary<(string Node, int VmId), ProxmoxVmInfo> proxmoxIndex =
                proxmoxVms.ToDictionary(
                    v => (v.Node, v.VmId),
                    v => v);

            await SyncDisabledVmsDeletionAsync(dbVms, proxmoxIndex, cancellationToken);
            await SyncMissingProxmoxVmsAsync(dbVms, proxmoxIndex, cancellationToken);
            await SyncProxmoxOnlyVmsAsync(dbVms, proxmoxIndex, cancellationToken);
            await SyncNameAndStatusAsync(dbVms, proxmoxIndex, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "VM inventory sync completed in {ElapsedMs} ms (processed {VmCount} DB VMs, {ProxmoxCount} Proxmox VMs).",
                stopwatch.ElapsedMilliseconds,
                dbVms.Count,
                proxmoxVms.Count);
        }

        private async Task SyncMissingProxmoxVmsAsync(
            List<Vm> dbVms,
            Dictionary<(string Node, int VmId), ProxmoxVmInfo> proxmoxIndex,
            CancellationToken ct)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan deleteGrace = TimeSpan.FromDays(30);

            foreach (Vm vm in dbVms)
            {
                (string Node, int VmId) key = (vm.Node, vm.VmId);
                bool existsInProxmox = proxmoxIndex.ContainsKey(key);

                if (!existsInProxmox)
                {
                    if (!vm.IsDisabled)
                    {
                        vm.IsDisabled = true;
                        vm.DisabledAt = now;
                        _logger.LogWarning("VM {Name} ({VmId}) on node {Node} missing in Proxmox; marking as disabled.",
                            vm.Name, vm.VmId, vm.Node);
                    }
                    else if (!vm.IsDeleted && vm.DisabledAt.HasValue && now - vm.DisabledAt.Value > deleteGrace)
                    {
                        vm.IsDeleted = true;
                        vm.DeletedAt = now;
                        _logger.LogWarning("VM {Name} ({VmId}) on node {Node} disabled for > {Days} days; marking as deleted.",
                            vm.Name, vm.VmId, vm.Node, deleteGrace.TotalDays);
                    }
                }
            }

            await _db.SaveChangesAsync(ct);
        }

        private async Task SyncProxmoxOnlyVmsAsync(
            List<Vm> dbVms,
            Dictionary<(string Node, int VmId), ProxmoxVmInfo> proxmoxIndex,
            CancellationToken ct)
        {
            HashSet<(string Node, int VmId)> dbIndex = dbVms
                .Select(v => (v.Node, v.VmId))
                .ToHashSet();

            foreach (ProxmoxVmInfo proxVm in proxmoxIndex.Values)
            {
                if (!dbIndex.Contains((proxVm.Node, proxVm.VmId)))
                {
                    // A VM exists in Proxmox but not in our DB.
                    // For now, just log it so admins can investigate.
                    _logger.LogWarning("Proxmox VM not tracked in database: {Name} ({VmId}) on {Node}",
                        proxVm.Name, proxVm.VmId, proxVm.Node);
                }
            }

            await Task.CompletedTask;
        }

        private async Task SyncNameAndStatusAsync(
            List<Vm> dbVms,
            Dictionary<(string Node, int VmId), ProxmoxVmInfo> proxmoxIndex,
            CancellationToken ct)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            int updated = 0;

            foreach (Vm vm in dbVms)
            {
                (string Node, int VmId) key = (vm.Node, vm.VmId);
                if (!proxmoxIndex.TryGetValue(key, out ProxmoxVmInfo? proxVm) || proxVm == null)
                {
                    continue;
                }

                bool changed = false;

                // Name sync
                if (!string.IsNullOrWhiteSpace(proxVm.Name) &&
                    !string.Equals(vm.Name, proxVm.Name, StringComparison.Ordinal))
                {
                    _logger.LogInformation("Sync: updating VM name from '{Old}' to '{New}' for {VmId}@{Node}",
                        vm.Name,
                        proxVm.Name,
                        vm.VmId,
                        vm.Node);

                    vm.Name = proxVm.Name;
                    changed = true;
                }

                // Status + uptime sync
                VmStatus oldStatus = vm.Status;
                VmStatus newStatus = proxVm.Status;

                if (oldStatus != newStatus && newStatus != VmStatus.Unknown)
                {
                    // If we are leaving Running, accumulate uptime
                    if (oldStatus == VmStatus.Running && vm.LastStatusChangeAt.HasValue)
                    {
                        TimeSpan delta = now - vm.LastStatusChangeAt.Value;
                        if (delta.TotalSeconds > 0)
                        {
                            vm.TotalRunTimeSeconds += (long)delta.TotalSeconds;
                        }
                    }

                    vm.Status = newStatus;
                    vm.LastStatusChangeAt = now;
                    changed = true;
                }

                // Resource sync
                if (proxVm.CpuCores > 0 && vm.CpuCores != proxVm.CpuCores)
                {
                    vm.CpuCores = proxVm.CpuCores;
                    changed = true;
                }

                if (proxVm.MemoryMiB > 0 && vm.MemoryMiB != proxVm.MemoryMiB)
                {
                    vm.MemoryMiB = proxVm.MemoryMiB;
                    changed = true;
                }

                if (proxVm.DiskGiB > 0 && vm.DiskGiB != proxVm.DiskGiB)
                {
                    vm.DiskGiB = proxVm.DiskGiB;
                    changed = true;
                }

                if (changed)
                {
                    vm.LastSyncedAt = now;
                    updated++;
                }
            }

            if (updated > 0)
            {
                _logger.LogInformation("Sync: updated name/status/resources/uptime for {Count} VMs.", updated);
                await _db.SaveChangesAsync(ct);
            }
        }

        private async Task SyncDisabledVmsDeletionAsync(
            List<Vm> dbVms,
            Dictionary<(string Node, int VmId), ProxmoxVmInfo> proxmoxIndex,
            CancellationToken ct)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan deleteGrace = TimeSpan.FromDays(30); // adjust as needed

            int deletedCount = 0;

            foreach (Vm vm in dbVms)
            {
                // Only process managed VMs: those that have a real owner
                if (vm.OwnerUserId == Guid.Empty)
                {
                    continue;
                }

                if (!vm.IsDisabled || vm.IsDeleted || !vm.DisabledAt.HasValue)
                {
                    continue;
                }

                if (now - vm.DisabledAt.Value <= deleteGrace)
                {
                    continue;
                }

                (string Node, int VmId) key = (vm.Node, vm.VmId);
                bool existsInProxmox = proxmoxIndex.ContainsKey(key);

                if (existsInProxmox && vm.VmId > 0)
                {
                    try
                    {
                        await _proxmox.DeleteVmAsync(vm.Node, vm.VmId, ct);
                    }
                    catch (HttpRequestException httpEx)
                    {
                        string message = httpEx.Message ?? string.Empty;

                        // Treat "config does not exist" as already deleted
                        if (message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning(
                                httpEx,
                                "Sync: Proxmox reports VM {VmId} on {Node} does not exist when deleting; treating as already deleted.",
                                vm.VmId,
                                vm.Node);
                        }
                        else
                        {
                            // For other errors, log and skip this VM for now
                            _logger.LogError(
                                httpEx,
                                "Sync: Failed to delete VM {VmId} on {Node} during disabled-VM cleanup.",
                                vm.VmId,
                                vm.Node);
                            continue;
                        }
                    }
                }

                vm.IsDeleted = true;
                vm.DeletedAt = now;
                vm.LastSyncedAt = now;
                deletedCount++;
            }

            if (deletedCount > 0)
            {
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Sync: Deleted {Count} disabled VMs from Proxmox and marked them as deleted.", deletedCount);
            }
        }
    }
}

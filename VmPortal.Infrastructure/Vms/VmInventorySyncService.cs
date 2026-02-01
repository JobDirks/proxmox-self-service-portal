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

            // 1. DB VMs (excluding logically deleted)
            List<Vm> dbVms = await _db.Vms
                .Where(v => !v.IsDeleted)
                .ToListAsync(cancellationToken);

            // 2. Proxmox VMs
            IReadOnlyList<ProxmoxVmInfo> proxmoxVms = await _proxmox.ListVmsAsync(cancellationToken);

            Dictionary<(string Node, int VmId), ProxmoxVmInfo> proxmoxIndex =
                proxmoxVms.ToDictionary(
                    v => (v.Node, v.VmId),
                    v => v);

            await SyncMissingProxmoxVmsAsync(dbVms, proxmoxIndex, cancellationToken);
            await SyncProxmoxOnlyVmsAsync(dbVms, proxmoxIndex, cancellationToken);
            await SyncNameAndStatusAsync(dbVms, proxmoxIndex, cancellationToken);

            _logger.LogInformation("VM inventory sync completed.");
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

                // Name sync: if Proxmox name differs and is non-empty, update DB
                if (!string.IsNullOrWhiteSpace(proxVm.Name) &&
                    !string.Equals(vm.Name, proxVm.Name, StringComparison.Ordinal))
                {
                    _logger.LogInformation("Updating VM name from '{Old}' to '{New}' for {VmId}@{Node}",
                        vm.Name, proxVm.Name, vm.VmId, vm.Node);
                    vm.Name = proxVm.Name;
                    changed = true;
                }

                // Status sync: optional; you already refresh status elsewhere, but we can update if unknown
                if (vm.Status == VmStatus.Unknown && proxVm.Status != VmStatus.Unknown)
                {
                    vm.Status = proxVm.Status;
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
                _logger.LogInformation("Updated name/status for {Count} VMs during sync.", updated);
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}

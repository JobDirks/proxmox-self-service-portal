using System.Threading;
using System.Threading.Tasks;
using VmPortal.Domain.Vms;
using System.Collections.Generic;

namespace VmPortal.Application.Proxmox
{
    public interface IProxmoxClient
    {
        Task<VmStatus> GetVmStatusAsync(string node, int vmId, CancellationToken ct = default);
        Task StartVmAsync(string node, int vmId, CancellationToken ct = default);
        Task StopVmAsync(string node, int vmId, CancellationToken ct = default);
        Task ShutdownVmAsync(string node, int vmId, CancellationToken ct = default);

        Task<ProxmoxCloneResult> CloneVmAsync(
            string node,
            int templateVmId,
            string newName,
            int cpuCores,
            int memoryMiB,
            int diskGiB,
            CancellationToken ct = default);

        Task<int> GetNextVmIdAsync(CancellationToken ct = default);

        Task ConfigureVmResourcesAsync(
            string node,
            int vmId,
            int cpuCores,
            int memoryMiB,
            CancellationToken ct = default);

        Task ResizeVmDiskAsync(
            string node,
            int vmId,
            int newDiskGiB,
            CancellationToken ct = default);

        Task RebootVmAsync(string node, int vmId, CancellationToken ct = default);
        Task ResetVmAsync(string node, int vmId, CancellationToken ct = default);
        Task PauseVmAsync(string node, int vmId, CancellationToken ct = default);
        Task ResumeVmAsync(string node, int vmId, CancellationToken ct = default);

        Task<ProxmoxVncProxyInfo> CreateVncProxyAsync(
            string node,
            int vmId,
            CancellationToken ct = default);

        public Task<string> GetLoginTicketAsync(CancellationToken ct = default);

        Task<IReadOnlyList<ProxmoxVmInfo>> ListVmsAsync(CancellationToken ct = default);

        public Task DeleteVmAsync(string node, int vmId, CancellationToken ct = default);
        public Task SetVmTagsAsync(string node, int vmId, IEnumerable<string> tags, CancellationToken ct = default);
    }
}

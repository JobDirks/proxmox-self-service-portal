using System.Threading;
using System.Threading.Tasks;
using VmPortal.Domain.Vms;

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
    }
}

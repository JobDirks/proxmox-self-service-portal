using System.Net.NetworkInformation;
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
        // Add more actions (Pause/Resume, Resize, CloneFromTemplate) later
    }
}

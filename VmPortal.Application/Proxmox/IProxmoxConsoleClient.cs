using System.Threading;
using System.Threading.Tasks;

namespace VmPortal.Application.Proxmox
{
    public interface IProxmoxConsoleClient
    {
        public Task<ProxmoxLoginResult> LoginAsync(CancellationToken ct = default);

        public Task<ProxmoxVncProxyInfo> CreateVncProxyAsync(
            string node,
            int vmId,
            ProxmoxLoginResult login,
            CancellationToken ct = default);
    }
}

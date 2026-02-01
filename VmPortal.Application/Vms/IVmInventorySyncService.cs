using System.Threading;
using System.Threading.Tasks;

namespace VmPortal.Application.Vms
{
    public interface IVmInventorySyncService
    {
        public Task SyncAsync(CancellationToken cancellationToken = default);
    }
}

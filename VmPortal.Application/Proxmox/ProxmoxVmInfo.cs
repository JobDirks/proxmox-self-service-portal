using VmPortal.Domain.Vms;

namespace VmPortal.Application.Proxmox
{
    public sealed class ProxmoxVmInfo
    {
        public string Node { get; set; } = string.Empty;

        public int VmId { get; set; }

        public string Name { get; set; } = string.Empty;

        public VmStatus Status { get; set; } = VmStatus.Unknown;
    }
}

using System;

namespace VmPortal.Application.Proxmox
{
    public sealed class ProxmoxCloneResult
    {
        public int VmId { get; set; }
        public string TaskId { get; set; } = string.Empty;
    }
}

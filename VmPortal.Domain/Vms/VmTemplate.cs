using System;

namespace VmPortal.Domain.Vms
{
    public class VmTemplate
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;       // Display name in UI
        public string Node { get; set; } = string.Empty;       // Proxmox node name
        public int TemplateVmId { get; set; }                  // Source template VMID in Proxmox

        public int DefaultCpuCores { get; set; }
        public int DefaultMemoryMiB { get; set; }
        public int DefaultDiskGiB { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

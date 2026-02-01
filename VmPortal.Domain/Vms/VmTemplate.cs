using System;

namespace VmPortal.Domain.Vms
{
    public sealed class VmTemplate
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;       // Display name in UI
        public string Node { get; set; } = string.Empty;       // Proxmox node name
        public int TemplateVmId { get; set; }                  // Source template VMID in Proxmox
        public string? TemplateTagName { get; set; }

        public int DefaultCpuCores { get; set; }
        public int DefaultMemoryMiB { get; set; }
        public int DefaultDiskGiB { get; set; }
        public int? MaxCpuCores { get; set; }
        public int? MaxMemoryMiB { get; set; }
        public int? MaxDiskGiB { get; set; }

        public bool IsActive { get; set; } = true;
        public string? Description { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

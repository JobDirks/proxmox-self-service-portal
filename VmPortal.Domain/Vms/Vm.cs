using System;
using System.Collections.Generic;
using System.Text;
using VmPortal.Domain.Users;

namespace VmPortal.Domain.Vms
{
    public class Vm
    {
        public Guid Id { get; set; }
        public int VmId { get; set; }                 // Proxmox VMID
        public string Node { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public VmStatus Status { get; set; } = VmStatus.Stopped;
        public int CpuCores { get; set; }
        public int MemoryMiB { get; set; }
        public int DiskGiB { get; set; }
        public Guid? TemplateId { get; set; }
        public int? MaxCpuCores { get; set; }
        public int? MaxMemoryMiB { get; set; }
        public int? MaxDiskGiB { get; set; }
        public DateTimeOffset LastSyncedAt { get; set; } = DateTimeOffset.UtcNow;
        public int? TemplateVmId { get; set; }

        public Guid OwnerUserId { get; set; }
        public User? Owner { get; set; }
    }
}

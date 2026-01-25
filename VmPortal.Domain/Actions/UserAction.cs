using System;
using System.Collections.Generic;
using System.Text;

namespace VmPortal.Domain.Actions
{
    public class UserAction
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string ActionName { get; set; } = string.Empty;   // StartVm | StopVm | RequestStorage | etc.
        public string TargetType { get; set; } = string.Empty;   // Vm | Template | User
        public string TargetId { get; set; } = string.Empty;     // VMID or Guid or composite key
        public string MetadataJson { get; set; } = string.Empty; // Extra details
        public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

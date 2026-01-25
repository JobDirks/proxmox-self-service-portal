using System;
using System.Collections.Generic;
using VmPortal.Domain.Vms;

namespace VmPortal.Domain.Users
{
    public class User
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsActive { get; set; } = true;

        // Simplify collection initialization per IDE0028
        public ICollection<Vm> Vms { get; set; } = [];
    }
}

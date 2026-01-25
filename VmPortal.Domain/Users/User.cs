using System;
using System.Collections.Generic;
using System.Text;
using VmPortal.Domain.Vms;

namespace VmPortal.Domain.Users
{
    public class User
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; } = string.Empty; // SSO subject/objectId
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";         // Employee | Admin | Support
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public bool IsActive { get; set; } = true;

        public ICollection<Vm> Vms { get; set; } = new List<Vm>();
    }
}

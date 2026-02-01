using System;

namespace VmPortal.Domain.Vms
{
    public sealed class VmUserPermission
    {
        public Guid Id { get; set; }

        public Guid VmId { get; set; }

        public Guid UserId { get; set; }

        public bool IsPrimaryOwner { get; set; }

        public bool CanDelete { get; set; }

        public bool CanManageResources { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}

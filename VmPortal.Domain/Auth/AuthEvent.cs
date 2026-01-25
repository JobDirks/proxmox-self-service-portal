using System;
using System.Collections.Generic;
using System.Text;

namespace VmPortal.Domain.Auth
{
    public class AuthEvent
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string EventType { get; set; } = string.Empty; // login | failed | logout | refresh
        public string SourceIP { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    }
}

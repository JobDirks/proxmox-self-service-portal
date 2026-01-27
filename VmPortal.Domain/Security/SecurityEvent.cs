using System;
using System.Collections.Generic;
using System.Text;

namespace VmPortal.Domain.Security
{
    public class SecurityEvent
    {
        public Guid Id { get; set; }
        public string EventType { get; set; } = string.Empty; // LoginSuccess, LoginFailed, SessionInvalid, etc.
        public string UserId { get; set; } = string.Empty;     // ExternalId or Anonymous
        public string IpAddress { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;    // JSON metadata
        public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
        public string Severity { get; set; } = "Information";  // Information, Warning, Error, Critical
    }
}

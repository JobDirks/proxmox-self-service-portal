using System;

namespace VmPortal.Application.Console
{
    public sealed class ConsoleSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string Node { get; set; } = string.Empty;
        public int VmId { get; set; }
        public int Port { get; set; }
        public string Ticket { get; set; } = string.Empty;
        public string LoginTicket { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAt { get; set; }
        public string OwnerExternalId { get; set; } = string.Empty;
    }
}

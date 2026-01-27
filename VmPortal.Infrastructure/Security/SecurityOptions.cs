using System;
using System.Collections.Generic;
using System.Text;

namespace VmPortal.Infrastructure.Security
{
    public class SecurityOptions
    {
        public string SessionSecretKey { get; set; } = string.Empty;
        public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours
        public bool EncryptSensitiveFields { get; set; } = true;
        public string SqlitePassword { get; set; } = string.Empty;
        public bool EnableSecurityLogging { get; set; } = true;
    }
}

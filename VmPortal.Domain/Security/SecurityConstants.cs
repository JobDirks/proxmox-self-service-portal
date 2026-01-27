using System;
using System.Collections.Generic;
using System.Text;

namespace VmPortal.Domain.Security
{
    public static class SecurityConstants
    {
        public static class ClaimTypes
        {
            public const string ObjectId = "http://schemas.microsoft.com/identity/claims/objectidentifier";
            public const string PreferredUsername = "preferred_username";
            public const string Roles = "roles";
            public const string SessionHash = "session_hash";
        }

        public static class Policies
        {
            public const string Admin = "AdminPolicy";
            public const string Employee = "EmployeePolicy";
            public const string SecureSession = "SecureSessionPolicy";
        }

        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Employee = "Employee";
        }

        public static class Session
        {
            public const int DefaultTimeoutMinutes = 480; // 8 hours
            public const int ExtendedTimeoutMinutes = 1440; // 24 hours
            public const string HashAlgorithm = "HMACSHA256";
        }

        public static class Security
        {
            public const string DataProtectionPurpose = "VmPortal.PII";
            public const string SessionDataProtectionPurpose = "VmPortal.Session";
            public const int MinPasswordLength = 12;
            public const int SessionHashLength = 32;
        }
    }
}

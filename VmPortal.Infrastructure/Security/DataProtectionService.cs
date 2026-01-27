using Microsoft.AspNetCore.DataProtection;
using System;
using System.Security.Cryptography;
using VmPortal.Application.Security;
using VmPortal.Domain.Security;

namespace VmPortal.Infrastructure.Security
{
    internal class DataProtectionService : IDataProtectionService
    {
        private readonly IDataProtector _piiProtector;
        private readonly IDataProtector _sessionProtector;

        public DataProtectionService(IDataProtectionProvider dataProtectionProvider)
        {
            _piiProtector = dataProtectionProvider.CreateProtector(SecurityConstants.Security.DataProtectionPurpose);
            _sessionProtector = dataProtectionProvider.CreateProtector(SecurityConstants.Security.SessionDataProtectionPurpose);
        }

        public string ProtectPII(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            return _piiProtector.Protect(plainText);
        }

        public string UnprotectPII(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText)) return string.Empty;
            try
            {
                return _piiProtector.Unprotect(protectedText);
            }
            catch (CryptographicException)
            {
                return string.Empty; // Return empty for tampered/invalid data
            }
        }

        public string ProtectSession(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;
            return _sessionProtector.Protect(plainText);
        }

        public string UnprotectSession(string protectedText)
        {
            if (string.IsNullOrEmpty(protectedText)) return string.Empty;
            try
            {
                return _sessionProtector.Unprotect(protectedText);
            }
            catch (CryptographicException)
            {
                return string.Empty;
            }
        }

        public bool IsProtected(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;

            try
            {
                _piiProtector.Unprotect(value);
                return true;
            }
            catch (CryptographicException)
            {
                return false;
            }
        }
    }
}

using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using VmPortal.Application.Security;
using VmPortal.Domain.Security;

namespace VmPortal.Infrastructure.Security
{
    internal class SecureSessionManager : ISecureSessionManager
    {
        private readonly IMemoryCache _cache;
        private readonly IDataProtectionService _dataProtection;
        private readonly SecurityOptions _options;

        public SecureSessionManager(
            IMemoryCache cache,
            IDataProtectionService dataProtection,
            IOptions<SecurityOptions> options)
        {
            _cache = cache;
            _dataProtection = dataProtection;
            _options = options.Value;
        }

        public string GenerateSessionHash(ClaimsPrincipal principal, string sessionId)
        {
            string userId = principal.FindFirst(SecurityConstants.ClaimTypes.ObjectId)?.Value ?? "anonymous";
            string email = principal.FindFirst(SecurityConstants.ClaimTypes.PreferredUsername)?.Value ?? "";
            string roles = string.Join(",", principal.FindAll(ClaimTypes.Role).Select(c => c.Value));
            string timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmm");

            string payload = $"{userId}|{email}|{roles}|{sessionId}|{timestamp}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SessionSecretKey));
            byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hashBytes);
        }

        public async Task<bool> ValidateSessionAsync(ClaimsPrincipal principal, string sessionId)
        {
            string cacheKey = $"session:{sessionId}";
            SessionInfo? sessionInfo = _cache.Get<SessionInfo>(cacheKey);

            if (sessionInfo == null)
            {
                return false;
            }

            if (sessionInfo.ExpiresAt < DateTimeOffset.UtcNow)
            {
                await InvalidateSessionAsync(sessionId);
                return false;
            }

            string expectedHash = GenerateSessionHash(principal, sessionId);
            return sessionInfo.Hash == expectedHash;
        }

        public Task InvalidateSessionAsync(string sessionId)
        {
            string cacheKey = $"session:{sessionId}";
            _cache.Remove(cacheKey);
            return Task.CompletedTask;
        }

        public Task<bool> IsSessionExpiredAsync(string sessionId)
        {
            string cacheKey = $"session:{sessionId}";
            SessionInfo? sessionInfo = _cache.Get<SessionInfo>(cacheKey);
            bool expired = sessionInfo?.ExpiresAt < DateTimeOffset.UtcNow;
            return Task.FromResult(sessionInfo == null || expired);
        }

        public Task ExtendSessionAsync(string sessionId, int additionalMinutes = 60)
        {
            string cacheKey = $"session:{sessionId}";
            SessionInfo? sessionInfo = _cache.Get<SessionInfo>(cacheKey);

            if (sessionInfo != null)
            {
                sessionInfo.ExpiresAt = sessionInfo.ExpiresAt.AddMinutes(additionalMinutes);
                _cache.Set(cacheKey, sessionInfo, sessionInfo.ExpiresAt);
            }

            return Task.CompletedTask;
        }

        private class SessionInfo
        {
            public string Hash { get; set; } = string.Empty;
            public DateTimeOffset ExpiresAt { get; set; }
        }
    }
}

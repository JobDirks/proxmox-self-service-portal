using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using VmPortal.Application.Security;

namespace VmPortal.Infrastructure.Security
{
    internal class RateLimitingService(IMemoryCache cache) : IRateLimitingService
    {
        private readonly TimeSpan _defaultWindow = TimeSpan.FromMinutes(5);

        public Task<bool> CheckRateLimitAsync(string userId, string operation, int maxAttempts = 5, TimeSpan? window = null)
        {
            TimeSpan windowSpan = window ?? _defaultWindow;
            string cacheKey = $"ratelimit:{userId}:{operation}";

            RateLimitInfo? attempts = cache.Get<RateLimitInfo>(cacheKey);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (attempts == null)
            {
                // First attempt
                cache.Set(cacheKey, new RateLimitInfo
                {
                    Count = 1,
                    WindowStart = now,
                    LastAttempt = now
                }, windowSpan);
                return Task.FromResult(true);
            }

            // Check if we're still in the same window
            if (now - attempts.WindowStart > windowSpan)
            {
                // Reset window
                cache.Set(cacheKey, new RateLimitInfo
                {
                    Count = 1,
                    WindowStart = now,
                    LastAttempt = now
                }, windowSpan);
                return Task.FromResult(true);
            }

            // Within the window, check limit
            if (attempts.Count >= maxAttempts)
            {
                return Task.FromResult(false);
            }

            // Update counter
            attempts.Count++;
            attempts.LastAttempt = now;
            cache.Set(cacheKey, attempts, windowSpan);

            return Task.FromResult(true);
        }

        public Task ResetRateLimitAsync(string userId, string operation)
        {
            string cacheKey = $"ratelimit:{userId}:{operation}";
            cache.Remove(cacheKey);
            return Task.CompletedTask;
        }

        public Task<TimeSpan?> GetRemainingCooldownAsync(string userId, string operation)
        {
            string cacheKey = $"ratelimit:{userId}:{operation}";
            RateLimitInfo? attempts = cache.Get<RateLimitInfo>(cacheKey);

            if (attempts == null) return Task.FromResult<TimeSpan?>(null);

            TimeSpan elapsed = DateTimeOffset.UtcNow - attempts.WindowStart;
            TimeSpan remaining = _defaultWindow - elapsed;

            return Task.FromResult<TimeSpan?>(remaining > TimeSpan.Zero ? remaining : null);
        }

        private class RateLimitInfo
        {
            public int Count { get; set; }
            public DateTimeOffset WindowStart { get; set; }
            public DateTimeOffset LastAttempt { get; set; }
        }
    }
}

using System;
using System.Threading.Tasks;

namespace VmPortal.Application.Security
{
    public interface IRateLimitingService
    {
        Task<bool> CheckRateLimitAsync(string userId, string operation, int maxAttempts = 5, TimeSpan? window = null);
        Task ResetRateLimitAsync(string userId, string operation);
        Task<TimeSpan?> GetRemainingCooldownAsync(string userId, string operation);
    }
}

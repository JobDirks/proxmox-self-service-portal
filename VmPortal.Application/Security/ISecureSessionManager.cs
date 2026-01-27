using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Claims;
using System.Threading.Tasks;

namespace VmPortal.Application.Security
{
    public interface ISecureSessionManager
    {
        string GenerateSessionHash(ClaimsPrincipal principal, string sessionId);
        Task<bool> ValidateSessionAsync(ClaimsPrincipal principal, string sessionId);
        Task InvalidateSessionAsync(string sessionId);
        Task<bool> IsSessionExpiredAsync(string sessionId);
        Task ExtendSessionAsync(string sessionId, int additionalMinutes = 60);
    }
}

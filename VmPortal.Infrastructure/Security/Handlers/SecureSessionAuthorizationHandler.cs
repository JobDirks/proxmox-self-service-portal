using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using VmPortal.Application.Security;
using VmPortal.Application.Security.Requirements;

namespace VmPortal.Infrastructure.Security.Handlers
{
    public class SecureSessionAuthorizationHandler(
        ISecureSessionManager sessionManager,
        ISecurityEventLogger securityLogger,
        IHttpContextAccessor httpContextAccessor,
        ILogger<SecureSessionAuthorizationHandler> logger) : AuthorizationHandler<SecureSessionRequirement>
    {
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SecureSessionRequirement requirement)
        {
            HttpContext? httpContext = httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                context.Fail();
                return;
            }

            string? sessionId = httpContext.Session.Id;
            if (string.IsNullOrEmpty(sessionId))
            {
                context.Fail();
                await LogSessionViolation(context.User.Identity?.Name ?? "anonymous", "NoSessionId");
                return;
            }

            bool isExpired = await sessionManager.IsSessionExpiredAsync(sessionId);
            if (isExpired)
            {
                context.Fail();
                await LogSessionViolation(context.User.Identity?.Name ?? "anonymous", "SessionExpired");
                return;
            }

            if (requirement.RequireSessionHash)
            {
                bool isValid = await sessionManager.ValidateSessionAsync(context.User, sessionId);
                if (!isValid)
                {
                    context.Fail();
                    await LogSessionViolation(context.User.Identity?.Name ?? "anonymous", "InvalidSessionHash");
                    return;
                }
            }

            context.Succeed(requirement);
        }

        private async Task LogSessionViolation(string userId, string reason)
        {
            try
            {
                await securityLogger.LogSessionViolationAsync(userId,
                    httpContextAccessor.HttpContext?.Session.Id ?? "unknown", reason);
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "Failed to log session violation for user {UserId}", userId);
            }
        }
    }
}

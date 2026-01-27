using System.Security.Claims;
using VmPortal.Domain.Security;

namespace VmPortal.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetPreferredUsername(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(SecurityConstants.ClaimTypes.PreferredUsername)?.Value ?? string.Empty;
        }

        public static string[] GetRoles(this ClaimsPrincipal principal)
        {
            return [.. principal.FindAll(ClaimTypes.Role).Select(c => c.Value)];
        }

        public static bool HasSecureSession(this ClaimsPrincipal principal)
        {
            return principal.HasClaim(claim =>
                claim.Type == SecurityConstants.ClaimTypes.SessionHash &&
                !string.IsNullOrEmpty(claim.Value));
        }
    }
}

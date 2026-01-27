using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace VmPortal.Web.Middleware
{
    public class SecurityHeadersMiddleware(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            // Content Security Policy
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self'; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'");

            // Strict Transport Security
            context.Response.Headers.Append("Strict-Transport-Security",
                "max-age=31536000; includeSubDomains");

            // Frame protection
            context.Response.Headers.Append("X-Frame-Options", "DENY");

            // Content type protection
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

            // XSS protection
            context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

            // Referrer policy
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

            // Permissions policy
            context.Response.Headers.Append("Permissions-Policy",
                "camera=(), microphone=(), geolocation=()");

            await next(context);
        }
    }
}

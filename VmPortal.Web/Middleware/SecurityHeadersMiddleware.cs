using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace VmPortal.Web.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            bool isNoVnc = context.Request.Path.StartsWithSegments("/novnc", StringComparison.OrdinalIgnoreCase);

            string csp =
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self'; " +
                "connect-src 'self'; ";

            if (isNoVnc)
            {
                // Allow this app to frame its own /novnc pages (for the console iframe)
                csp += "frame-ancestors 'self'";
                context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            }
            else
            {
                // Default: no framing anywhere
                csp += "frame-ancestors 'none'";
                context.Response.Headers["X-Frame-Options"] = "DENY";
            }

            context.Response.Headers["Content-Security-Policy"] = csp;

            // Strict Transport Security
            context.Response.Headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains";

            // Content type protection
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";

            // XSS protection
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

            // Referrer policy
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // Permissions policy
            context.Response.Headers["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=()";

            await _next(context);
        }
    }
}

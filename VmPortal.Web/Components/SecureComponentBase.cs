using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using VmPortal.Application.Security;
using VmPortal.Domain.Security;
using VmPortal.Web.Extensions;

namespace VmPortal.Web.Components
{
    public abstract class SecureComponentBase : ComponentBase
    {
        [Inject] protected AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;
        [Inject] protected ISecurityEventLogger SecurityLogger { get; set; } = default!;
        [Inject] protected ILogger<SecureComponentBase> Logger { get; set; } = default!;

        protected ClaimsPrincipal? CurrentUser { get; private set; }
        protected string CurrentUserId { get; private set; } = string.Empty;
        protected string CurrentUserEmail { get; private set; } = string.Empty;
        protected bool IsAdmin { get; private set; }
        protected bool IsEmployee { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            AuthenticationState authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            CurrentUser = authState.User;

            if (CurrentUser?.Identity?.IsAuthenticated == true)
            {
                // Use Microsoft.Identity.Web extensions to avoid conflicts
                CurrentUserId = CurrentUser.GetObjectId() ?? string.Empty;
                CurrentUserEmail = CurrentUser.GetPreferredUsername();
                IsAdmin = CurrentUser.IsInRole(SecurityConstants.Roles.Admin);
                IsEmployee = CurrentUser.IsInRole(SecurityConstants.Roles.Employee) || IsAdmin;
            }

            await base.OnInitializedAsync();
        }

        protected async Task LogSecurityEventAsync(string eventType, string details = "", string severity = "Information")
        {
            try
            {
                await SecurityLogger.LogSecurityEventAsync(new SecurityEvent
                {
                    Id = Guid.NewGuid(),
                    EventType = eventType,
                    UserId = CurrentUserId,
                    Details = details,
                    Severity = severity,
                    OccurredAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
            }
        }

        protected async Task LogUnauthorizedAccessAsync(string resource)
        {
            await LogSecurityEventAsync("UnauthorizedAccess", $"Resource: {resource}", "Warning");
        }
    }
}

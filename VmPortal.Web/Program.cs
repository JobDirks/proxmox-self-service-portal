using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using VmPortal.Web.Components;
using VmPortal.Infrastructure;
using VmPortal.Infrastructure.Data;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Blazor Web App (Interactive Server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Authentication + Authorization (Entra ID)
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.ResponseType = "code";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async ctx =>
            {
                ILogger<Program> log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                VmPortalDbContext db = ctx.HttpContext.RequestServices.GetRequiredService<VmPortalDbContext>();
                ClaimsPrincipal principal = ctx.Principal!;
                ClaimsIdentity identity = (ClaimsIdentity)principal.Identity!;

                // Normalize roles: copy any “roles” claims into ClaimTypes.Role so [Authorize(Roles="Admin")] works
                var roleValues = identity.FindAll("roles").Select(c => c.Value).ToList();
                var existingRoleValues = identity.FindAll(ClaimTypes.Role).Select(c => c.Value);
                foreach (string r in roleValues.Except(existingRoleValues))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, r));
                }
                log.LogInformation("Roles in principal: {roles}", identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray());

                // User upsert (no local role storage)
                string externalId =
                    principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                    ?? principal.FindFirst("sub")?.Value
                    ?? string.Empty;
                string email =
                    principal.FindFirst("preferred_username")?.Value
                    ?? principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? string.Empty;
                string name = principal.FindFirst(ClaimTypes.Name)?.Value ?? email;
                string username = email;

                if (!string.IsNullOrEmpty(externalId))
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId);
                    if (user == null)
                    {
                        user = new VmPortal.Domain.Users.User
                        {
                            Id = Guid.NewGuid(),
                            ExternalId = externalId,
                            Username = username,
                            Email = email,
                            DisplayName = name,
                            CreatedAt = DateTimeOffset.UtcNow,
                            IsActive = true
                        };
                        db.Users.Add(user);
                    }
                    else
                    {
                        user.Username = username;
                        user.Email = email;
                        user.DisplayName = name;
                    }
                    await db.SaveChangesAsync();
                }
            }
        };
    });
// Prefer default RoleClaimType to ClaimTypes.Role after normalization
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
    options.TokenValidationParameters.NameClaimType = "name";
});

// Authorization builder for Admin policy and fallback (authenticated users)
var auth = builder.Services.AddAuthorizationBuilder();
auth.AddPolicy("Admin", p => p.RequireRole("Admin"));
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
});

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/login", async ctx =>
{
    await ctx.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
}).AllowAnonymous();

app.MapGet("/logout", async ctx =>
{
    await ctx.SignOutAsync();
    await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
}).RequireAuthorization();

app.MapHealthChecks("/health");

app.Run();

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using System.Net.WebSockets;
using System.Security.Claims;
using VmPortal.Application.Console;
using VmPortal.Application.Proxmox;
using VmPortal.Application.Security;
using VmPortal.Application.Security.Requirements;
using VmPortal.Application.Vms;
using VmPortal.Domain.Security;
using VmPortal.Domain.Users;
using VmPortal.Domain.Vms;
using VmPortal.Infrastructure;
using VmPortal.Infrastructure.Data;
using VmPortal.Infrastructure.Proxmox;
using VmPortal.Web.Components;
using VmPortal.Web.Extensions;
using VmPortal.Web.Middleware;
using VmPortal.Web.WebSockets;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Blazor Web App (Interactive Server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Session support for secure session management
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(480); // 8 hours
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Infrastructure (DbContext, Proxmox, Security services)
builder.Services.AddInfrastructure(builder.Configuration);

// HTTP Context Accessor for authorization handlers
builder.Services.AddHttpContextAccessor();

// VM Resource Limits configuration
builder.Services.Configure<VmResourceLimitsOptions>(
    builder.Configuration.GetSection("VmResourceLimits"));

// Authentication + Authorization (Entra ID with enhanced security)
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
                ISecurityEventLogger securityLogger = ctx.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>();
                ISecureSessionManager sessionManager = ctx.HttpContext.RequestServices.GetRequiredService<ISecureSessionManager>();

                ClaimsPrincipal principal = ctx.Principal!;
                ClaimsIdentity identity = (ClaimsIdentity)principal.Identity!;

                // Normalize roles: copy "roles" claims to ClaimTypes.Role
                string[] roleValues = [.. identity.FindAll("roles").Select(c => c.Value)];
                string[] existingRoleValues = [.. identity.FindAll(ClaimTypes.Role).Select(c => c.Value)];
                foreach (string role in roleValues.Except(existingRoleValues))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }

                // Generate secure session hash
                string sessionId = ctx.HttpContext.Session.Id;
                string sessionHash = sessionManager.GenerateSessionHash(principal, sessionId);
                identity.AddClaim(new Claim(SecurityConstants.ClaimTypes.SessionHash, sessionHash));

                log.LogInformation("User authenticated: {UserId} with roles: {Roles}",
                    principal.GetObjectId(), string.Join(", ", principal.GetRoles()));

                // User upsert (no local role storage)
                string? externalId = principal.GetObjectId();
                string email = principal.GetPreferredUsername();
                string name = principal.GetDisplayName() ?? email;
                string username = email;

                if (!string.IsNullOrEmpty(externalId))
                {
                    VmPortal.Domain.Users.User? user = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId);
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
                        await securityLogger.LogSecurityEventAsync(new SecurityEvent
                        {
                            Id = Guid.NewGuid(),
                            EventType = "UserCreated",
                            UserId = externalId,
                            IpAddress = ctx.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                            Details = $"Email: {email}, Name: {name}",
                            Severity = "Information"
                        });
                    }
                    else
                    {
                        user.Username = username;
                        user.Email = email;
                        user.DisplayName = name;
                    }

                    await db.SaveChangesAsync();

                    // Log successful authentication
                    await securityLogger.LogLoginAttemptAsync(
                        externalId,
                        ctx.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                        true);
                }
            },

            OnAuthenticationFailed = async ctx =>
            {
                ISecurityEventLogger securityLogger = ctx.HttpContext.RequestServices.GetRequiredService<ISecurityEventLogger>();
                await securityLogger.LogLoginAttemptAsync(
                    "unknown",
                    ctx.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    false,
                    ctx.Exception?.Message ?? "Authentication failed");
            }
        };
    });

// Enhanced authorization with secure session policies
builder.Services.Configure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
    options.TokenValidationParameters.NameClaimType = "name";
});

builder.Services.AddAuthorization(options =>
{
    // Fallback policy (all authenticated users)
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // Admin policy
    options.AddPolicy(SecurityConstants.Policies.Admin, policy =>
        policy.RequireRole(SecurityConstants.Roles.Admin));

    // Employee policy (explicit)
    options.AddPolicy(SecurityConstants.Policies.Employee, policy =>
        policy.RequireAuthenticatedUser()
        .RequireAssertion(ctx =>
            ctx.User.IsInRole(SecurityConstants.Roles.Employee) ||
            ctx.User.IsInRole(SecurityConstants.Roles.Admin)));

    // Secure session policy
    options.AddPolicy(SecurityConstants.Policies.SecureSession, policy =>
        policy.RequireAuthenticatedUser()
        .AddRequirements(new SecureSessionRequirement()));
});

WebApplication app = builder.Build();

// Security middleware pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Custom security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// Session must come before Authentication
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Apply antiforgery only to non-API routes
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase),
    branch => branch.UseAntiforgery());

app.UseWebSockets();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Enhanced login/logout with security logging
app.MapGet("/login", async ctx =>
{
    await ctx.ChallengeAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
}).AllowAnonymous();

app.MapGet("/logout", async ctx =>
{
    ISecurityEventLogger? securityLogger = ctx.RequestServices.GetService<ISecurityEventLogger>();
    if (securityLogger != null && ctx.User?.Identity?.IsAuthenticated == true)
    {
        string? userId = ctx.User.GetObjectId();
        if (!string.IsNullOrEmpty(userId))
        {
            await securityLogger.LogSecurityEventAsync(new SecurityEvent
            {
                Id = Guid.NewGuid(),
                EventType = "UserLogout",
                UserId = userId,
                IpAddress = ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                Severity = "Information"
            });
        }
    }

    await ctx.SignOutAsync();
    await ctx.SignOutAsync(
        OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = "/" });
}).RequireAuthorization();

// Console session API: create a secure sessionId for VM console
app.MapPost("/api/console-session",
    async (Guid vmId,
           HttpContext httpContext,
           VmPortalDbContext db,
           IConsoleSessionService consoleSessionService,
           CancellationToken ct) =>
    {
        ClaimsPrincipal user = httpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Results.Json(new { error = "unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        bool isAdmin = user.IsInRole("Admin");

        string? externalId = user.GetObjectId();
        if (string.IsNullOrEmpty(externalId))
        {
            return Results.Json(new { error = "no_external_id" }, statusCode: StatusCodes.Status403Forbidden);
        }

        User? currentUser = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);
        if (currentUser == null)
        {
            return Results.Json(new { error = "user_not_in_db", externalId }, statusCode: StatusCodes.Status403Forbidden);
        }

        Vm? vm = await db.Vms.FirstOrDefaultAsync(v => v.Id == vmId, ct);
        if (vm == null)
        {
            return Results.Json(new { error = "vm_not_found" }, statusCode: StatusCodes.Status404NotFound);
        }

        if (!isAdmin && vm.OwnerUserId != currentUser.Id)
        {
            return Results.Json(new { error = "forbidden_not_owner" }, statusCode: StatusCodes.Status403Forbidden);
        }

        if (vm.VmId <= 0 || string.IsNullOrWhiteSpace(vm.Node))
        {
            return Results.Json(new { error = "vm_not_linked_to_proxmox" }, statusCode: StatusCodes.Status400BadRequest);
        }

        string nodeName = vm.Node.Trim();

        string sessionId;
        try
        {
            sessionId = await consoleSessionService.CreateSessionAsync(nodeName, vm.VmId, externalId, ct);
        }
        catch (Exception ex)
        {
            return Results.Json(
                new { error = "proxmox_error", details = ex.Message },
                statusCode: StatusCodes.Status502BadGateway);
        }

        return Results.Ok(new { sessionId });
    })
    .RequireAuthorization();

// WebSocket proxy: browser <-> app <-> Proxmox vncwebsocket
app.Map("/ws/console/{sessionId}", async (
    HttpContext context,
    string sessionId,
    IConsoleSessionService consoleSessionService,
    IOptions<ProxmoxOptions> proxmoxOptions) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket request expected.");
        return;
    }

    ClaimsPrincipal user = context.User;

    if (!user.Identity?.IsAuthenticated ?? true)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized.");
        return;
    }

    string? externalId = user.GetObjectId();
    bool isAdmin = user.IsInRole("Admin");

    if (string.IsNullOrEmpty(externalId))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("No external id.");
        return;
    }

    ConsoleSession? session = consoleSessionService.GetSession(sessionId);
    if (session == null)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Console session not found or expired.");
        return;
    }

    if (!isAdmin && !string.Equals(session.OwnerExternalId, externalId, StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsync("Forbidden: not session owner.");
        return;
    }

    // One-time use
    consoleSessionService.InvalidateSession(sessionId);

    ProxmoxOptions opt = proxmoxOptions.Value;
    string baseUrl = opt.BaseUrl.TrimEnd('/');

    UriBuilder proxmoxUriBuilder = new UriBuilder(baseUrl);

    if (string.Equals(proxmoxUriBuilder.Scheme, "https", StringComparison.OrdinalIgnoreCase))
    {
        proxmoxUriBuilder.Scheme = "wss";
    }
    else
    {
        proxmoxUriBuilder.Scheme = "ws";
    }

    proxmoxUriBuilder.Path = $"/api2/json/nodes/{Uri.EscapeDataString(session.Node)}/qemu/{session.VmId}/vncwebsocket";
    proxmoxUriBuilder.Query = $"port={session.Port}&vncticket={Uri.EscapeDataString(session.Ticket)}";

    using ClientWebSocket proxmoxSocket = new ClientWebSocket();

    if (opt.DevIgnoreCertErrors)
    {
        proxmoxSocket.Options.RemoteCertificateValidationCallback =
            (sender, certificate, chain, errors) => true;
    }

    // Use login ticket as PVEAuthCookie (login session)
    proxmoxSocket.Options.SetRequestHeader("Cookie", "PVEAuthCookie=" + session.LoginTicket);
    proxmoxSocket.Options.SetRequestHeader("Origin", baseUrl);

    try
    {
        await proxmoxSocket.ConnectAsync(proxmoxUriBuilder.Uri, context.RequestAborted);
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        await context.Response.WriteAsync("Failed to connect to Proxmox VNC WebSocket: " + ex.Message);
        return;
    }

    WebSocket clientSocket = await context.WebSockets.AcceptWebSocketAsync();

    try
    {
        await WebSocketProxy.ProxyAsync(clientSocket, proxmoxSocket, context.RequestAborted);
    }
    catch
    {
        // ignore; sockets closed in proxy
    }
});


// Debug endpoint to test VNC WebSocket connectivity
app.MapPost("/api/console-debug",
    async (Guid vmId,
           HttpContext httpContext,
           VmPortalDbContext db,
           IProxmoxClient proxmoxClient,
           IOptions<ProxmoxOptions> proxmoxOptions,
           CancellationToken ct) =>
    {
        ClaimsPrincipal user = httpContext.User;

        if (!user.Identity?.IsAuthenticated ?? true)
        {
            return Results.Json(new { ok = false, error = "unauthorized" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        string? externalId = user.GetObjectId();
        if (string.IsNullOrEmpty(externalId))
        {
            return Results.Json(new { ok = false, error = "no_external_id" }, statusCode: StatusCodes.Status403Forbidden);
        }

        User? currentUser = await db.Users.FirstOrDefaultAsync(u => u.ExternalId == externalId, ct);
        if (currentUser == null)
        {
            return Results.Json(new { ok = false, error = "user_not_in_db", externalId }, statusCode: StatusCodes.Status403Forbidden);
        }

        Vm? vm = await db.Vms.FirstOrDefaultAsync(v => v.Id == vmId, ct);
        if (vm == null)
        {
            return Results.Json(new { ok = false, error = "vm_not_found" }, statusCode: StatusCodes.Status404NotFound);
        }

        if (vm.VmId <= 0 || string.IsNullOrWhiteSpace(vm.Node))
        {
            return Results.Json(new { ok = false, error = "vm_not_linked_to_proxmox" }, statusCode: StatusCodes.Status400BadRequest);
        }

        string nodeName = vm.Node.Trim();

        ProxmoxOptions opt = proxmoxOptions.Value;
        string baseUrl = opt.BaseUrl.TrimEnd('/');

        // Step 1: create VNC proxy
        ProxmoxVncProxyInfo proxyInfo;
        try
        {
            proxyInfo = await proxmoxClient.CreateVncProxyAsync(nodeName, vm.VmId, ct);
        }
        catch (Exception ex)
        {
            return Results.Json(
                new
                {
                    ok = false,
                    error = "create_vncproxy_failed",
                    details = ex.Message
                },
                statusCode: StatusCodes.Status502BadGateway);
        }

        // Step 2: connect WebSocket to Proxmox vncwebsocket
        UriBuilder proxmoxUriBuilder = new UriBuilder(baseUrl);

        if (string.Equals(proxmoxUriBuilder.Scheme, "https", StringComparison.OrdinalIgnoreCase))
        {
            proxmoxUriBuilder.Scheme = "wss";
        }
        else
        {
            proxmoxUriBuilder.Scheme = "ws";
        }

        proxmoxUriBuilder.Path = $"/api2/json/nodes/{Uri.EscapeDataString(nodeName)}/vncwebsocket";
        proxmoxUriBuilder.Query = $"port={proxyInfo.Port}&vncticket={Uri.EscapeDataString(proxyInfo.Ticket)}";

        using ClientWebSocket ws = new ClientWebSocket();

        if (opt.DevIgnoreCertErrors)
        {
            ws.Options.RemoteCertificateValidationCallback =
                (sender, certificate, chain, errors) => true;
        }

        // Use API token auth header in documented form
        if (!string.IsNullOrWhiteSpace(opt.TokenId) && !string.IsNullOrWhiteSpace(opt.TokenSecret))
        {
            string authHeader = $"PVEAPIToken={opt.TokenId}={opt.TokenSecret}";
            ws.Options.SetRequestHeader("Authorization", authHeader);
        }

        ws.Options.SetRequestHeader("Origin", baseUrl);

        try
        {
            await ws.ConnectAsync(proxmoxUriBuilder.Uri, ct);
        }
        catch (Exception ex)
        {
            return Results.Json(
                new
                {
                    ok = false,
                    error = "connect_vncwebsocket_failed",
                    details = ex.ToString(),
                    uri = proxmoxUriBuilder.Uri.ToString(),
                    port = proxyInfo.Port,
                    ticket = proxyInfo.Ticket
                },
                statusCode: StatusCodes.Status502BadGateway);
        }

        try
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test completed", ct);
        }
        catch
        {
            // ignore
        }

        return Results.Json(
            new
            {
                ok = true,
                uri = proxmoxUriBuilder.Uri.ToString(),
                port = proxyInfo.Port,
                ticket = proxyInfo.Ticket
            });
    })
    .RequireAuthorization();

app.MapHealthChecks("/health");

app.Run();

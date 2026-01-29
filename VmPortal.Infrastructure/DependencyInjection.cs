using System;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using VmPortal.Application.Proxmox;
using VmPortal.Application.Security;
using VmPortal.Application.Security.Requirements;
using VmPortal.Infrastructure.Data;
using VmPortal.Infrastructure.Proxmox;
using VmPortal.Infrastructure.Security;
using VmPortal.Infrastructure.Security.Handlers;
using VmPortal.Application.Console;
using VmPortal.Infrastructure.Console;

namespace VmPortal.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Database connection (no password - use field-level encryption instead)
            string conn = configuration.GetConnectionString("Default") ?? "Data Source=./data/vmportal.db";
            services.AddDbContext<VmPortalDbContext>(options => options.UseSqlite(conn));

            // Health checks
            services.AddHealthChecks()
                    .AddDbContextCheck<VmPortalDbContext>("db");

            // Security services
            services.Configure<SecurityOptions>(configuration.GetSection("Security"));
            services.AddMemoryCache();

            // Data protection with EF Core key persistence (field-level encryption)
            services.AddDataProtection()
                    .PersistKeysToDbContext<VmPortalDbContext>();

            services.AddScoped<IDataProtectionService, DataProtectionService>();
            services.AddScoped<ISecureSessionManager, SecureSessionManager>();
            services.AddScoped<ISecurityEventLogger, SecurityEventLogger>();
            services.AddScoped<IRateLimitingService, RateLimitingService>();
            services.AddScoped<IInputSanitizer, InputSanitizer>();

            // Authorization handlers
            services.AddScoped<IAuthorizationHandler, SecureSessionAuthorizationHandler>();

            // Console session service
            services.AddMemoryCache();

            services.AddScoped<IConsoleSessionService, ConsoleSessionService>();

            // Proxmox client configuration
            services.Configure<ProxmoxOptions>(configuration.GetSection("Proxmox"));

            IAsyncPolicy<HttpResponseMessage> retry = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync([
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                ]);

            IAsyncPolicy<HttpResponseMessage> timeout = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(15));

            services.AddHttpClient<IProxmoxClient, ProxmoxClient>((sp, client) =>
            {
                Microsoft.Extensions.Options.IOptions<ProxmoxOptions> options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProxmoxOptions>>();
                string baseUrl = options.Value.BaseUrl.TrimEnd('/');
                client.BaseAddress = new Uri($"{baseUrl}/api2/json/");
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddPolicyHandler(retry)
            .AddPolicyHandler(timeout)
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                Microsoft.Extensions.Options.IOptions<ProxmoxOptions> options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProxmoxOptions>>();
                HttpClientHandler handler = new HttpClientHandler();
                if (options.Value.DevIgnoreCertErrors)
                {
                    handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }
                return handler;
            });

            return services;
        }
    }
}

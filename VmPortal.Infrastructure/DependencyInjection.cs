using System;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using VmPortal.Application.Proxmox;
using VmPortal.Infrastructure.Data;
using VmPortal.Infrastructure.Proxmox;

namespace VmPortal.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            string conn = configuration.GetConnectionString("Default") ?? "Data Source=./data/vmportal.db";
            services.AddDbContext<VmPortalDbContext>(options => options.UseSqlite(conn));

            // Health checks
            services.AddHealthChecks()
                    .AddDbContextCheck<VmPortalDbContext>("db");

            services.Configure<ProxmoxOptions>(configuration.GetSection("Proxmox"));

            // Resilience policies
            IAsyncPolicy<HttpResponseMessage> retry = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(2),
                    TimeSpan.FromSeconds(5)
                });

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

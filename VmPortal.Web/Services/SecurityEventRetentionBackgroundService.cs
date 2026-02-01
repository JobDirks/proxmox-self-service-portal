using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using VmPortal.Application.Security;
using VmPortal.Domain.Security;
using VmPortal.Infrastructure.Data;

namespace VmPortal.Web.Services
{
    internal sealed class SecurityEventRetentionBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SecurityEventRetentionBackgroundService> _logger;
        private readonly SecurityEventRetentionOptions _options;

        public SecurityEventRetentionBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<SecurityEventRetentionBackgroundService> logger,
            IOptions<SecurityEventRetentionOptions> options)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SecurityEvent retention background service started.");

            TimeSpan interval = TimeSpan.FromHours(
                _options.CleanupIntervalHours > 0 ? _options.CleanupIntervalHours : 24);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope scope = _serviceProvider.CreateScope();
                    VmPortalDbContext db = scope.ServiceProvider.GetRequiredService<VmPortalDbContext>();

                    DateTimeOffset now = DateTimeOffset.UtcNow;
                    int retentionDays = _options.RetentionDays > 0 ? _options.RetentionDays : 365;
                    DateTimeOffset cutoff = now.AddDays(-retentionDays);

                    // Load all events, then filter in memory to avoid translation issues
                    List<SecurityEvent> allEvents = await db.SecurityEvents.ToListAsync(stoppingToken);

                    List<SecurityEvent> oldEvents = allEvents
                        .Where(e => e.OccurredAt < cutoff)
                        .ToList();

                    int deletedCount = oldEvents.Count;

                    if (deletedCount > 0)
                    {
                        db.SecurityEvents.RemoveRange(oldEvents);
                        await db.SaveChangesAsync(stoppingToken);

                        _logger.LogInformation(
                            "SecurityEvent retention cleanup deleted {Count} events older than {Cutoff}.",
                            deletedCount,
                            cutoff);
                    }
                    else
                    {
                        _logger.LogDebug(
                            "SecurityEvent retention cleanup found no events older than {Cutoff}.",
                            cutoff);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during SecurityEvent retention cleanup.");
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // stopping
                }
            }

            _logger.LogInformation("SecurityEvent retention background service stopped.");
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VmPortal.Application.Vms;

namespace VmPortal.Web.Services
{
    internal sealed class VmInventorySyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VmInventorySyncBackgroundService> _logger;
        private readonly TimeSpan _interval;

        public VmInventorySyncBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<VmInventorySyncBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _interval = TimeSpan.FromHours(1); // run every hour; you can tweak this
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("VM inventory sync background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using IServiceScope scope = _serviceProvider.CreateScope();
                    IVmInventorySyncService syncService =
                        scope.ServiceProvider.GetRequiredService<IVmInventorySyncService>();

                    await syncService.SyncAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during VM inventory sync.");
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // service stopping
                }
            }

            _logger.LogInformation("VM inventory sync background service stopped.");
        }
    }
}

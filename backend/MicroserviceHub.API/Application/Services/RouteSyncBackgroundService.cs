using MicroserviceHub.API.Application.Interfaces;
using Serilog;
 
namespace MicroserviceHub.API.Application.Services
{
    /// <summary>
    /// Runs every 30 seconds in the background.
    /// Calls RouteSyncService.SyncAsync() to pull new/changed
    /// routes from APISix into the DB automatically.
    /// </summary>
    public class RouteSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan             _interval;
 
        public RouteSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            IConfiguration       config)
        {
            _scopeFactory = scopeFactory;
            // Configurable — default 30 seconds
            var seconds = config.GetValue<int>("RouteSync:IntervalSeconds", 30);
            _interval   = TimeSpan.FromSeconds(seconds);
        }
 
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Log.Information("RouteSyncBackgroundService started. Interval: {Interval}s", _interval.TotalSeconds);
 
            // Wait 10 seconds on startup so APISix has time to be ready
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
 
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope       = _scopeFactory.CreateScope();
                    var       syncService = scope.ServiceProvider.GetRequiredService<IRouteSyncService>();
 
                    var result = await syncService.SyncAsync();
 
                    if (result.Added.Count > 0)
                        Log.Information("RouteSyncBackgroundService: {Count} new route(s) added: {Routes}",
                            result.Added.Count, string.Join(", ", result.Added));
 
                    if (!result.Success && !string.IsNullOrEmpty(result.Error))
                        Log.Warning("RouteSyncBackgroundService: {Error}", result.Error);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "RouteSyncBackgroundService: Unexpected error during sync");
                }
 
                await Task.Delay(_interval, stoppingToken);
            }
 
            Log.Information("RouteSyncBackgroundService stopped.");
        }
    }
}
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Waitlists;

namespace TmsApi.Infrastructure.Workers;

/// <summary>Every minute: expire stale offers and hand free seats to the next waiting students.
/// State lives in PostgreSQL, so nothing is lost if the app restarts.</summary>
public class WaitlistMaintenanceWorker(IServiceScopeFactory scopeFactory, ILogger<WaitlistMaintenanceWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IWaitlistService>();
                var expired = await svc.ExpireOffersAsync(stoppingToken);
                await svc.ProcessOpenSeatsAsync(stoppingToken);
                if (expired > 0) logger.LogInformation("Expired {Count} waitlist offers", expired);
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception ex) { logger.LogError(ex, "Waitlist maintenance run failed"); }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer t, CancellationToken ct)
    {
        try { return await t.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
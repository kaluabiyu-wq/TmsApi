using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Notifications;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Workers;

public sealed class NotificationDeliveryWorker(IServiceScopeFactory scopes, ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    private const int MaxAttempts = 5;
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProcessBatchAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogError(ex, "Notification delivery worker failed while processing a batch."); }
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
        var senders = scope.ServiceProvider.GetServices<INotificationChannelSender>().ToDictionary(x => x.Channel, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        await db.NotificationDeliveries.Where(x => x.Status == "Processing" && x.NextAttemptAt != null && x.NextAttemptAt <= now)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Pending").SetProperty(x => x.NextAttemptAt, now), ct);

        var batch = await db.NotificationDeliveries.Include(x => x.Notification)
            .Where(x => x.Status == "Pending" && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
            .OrderBy(x => x.NextAttemptAt).ThenBy(x => x.Id).Take(25).ToListAsync(ct);

        foreach (var delivery in batch)
        {
            var leaseUntil = DateTime.UtcNow.AddMinutes(10);
            var claimed = await db.NotificationDeliveries.Where(x => x.Id == delivery.Id && x.Status == "Pending")
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Processing").SetProperty(x => x.NextAttemptAt, leaseUntil), ct);
            if (claimed == 0) continue;
            delivery.Status = "Processing"; // ExecuteUpdate bypasses tracked state.
            try
            {
                if (!senders.TryGetValue(delivery.Channel, out var sender)) throw new InvalidOperationException($"No sender is registered for channel '{delivery.Channel}'.");
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == delivery.Notification.UserId, ct)
                    ?? throw new InvalidOperationException("Notification recipient no longer exists.");
                await sender.SendAsync(delivery.Notification, user, ct);
                delivery.Status = "Sent"; delivery.SentAt = DateTime.UtcNow; delivery.LastError = null; delivery.NextAttemptAt = null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                delivery.Attempts++;
                delivery.LastError = ex.Message.Length > 1900 ? ex.Message[..1900] : ex.Message;
                delivery.Status = delivery.Attempts >= MaxAttempts ? "Failed" : "Pending";
                delivery.NextAttemptAt = delivery.Status == "Pending"
                    ? DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Pow(2, delivery.Attempts))) : null;
                logger.LogWarning(ex, "Notification delivery {DeliveryId} ({Channel}) failed on attempt {Attempt}.", delivery.Id, delivery.Channel, delivery.Attempts);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}

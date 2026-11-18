using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Notifications;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Notifications;

public sealed class NotificationService(TmsDbContext db, INotificationRealtimePublisher realtime) : INotificationService
{
    public async Task<NotificationResponse> CreateAsync(CreateNotificationRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserId)) throw new ArgumentException("A recipient user ID is required.");
        var title = request.Title?.Trim();
        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 180) throw new ArgumentException("Title is required and must be at most 180 characters.");
        if (string.IsNullOrWhiteSpace(message) || message.Length > 4000) throw new ArgumentException("Message is required and must be at most 4000 characters.");

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UserId, ct)
            ?? throw new KeyNotFoundException("Notification recipient was not found.");
        var category = string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim();
        if (category.Length > 80) throw new ArgumentException("Category must be at most 80 characters.");

        var preference = await db.NotificationPreferences.AsNoTracking()
            .Where(x => x.UserId == request.UserId && (x.Category == category || x.Category == "*"))
            .OrderByDescending(x => x.Category == category)
            .FirstOrDefaultAsync(ct);
        var inAppEnabled = preference?.InAppEnabled ?? true;
        var emailEnabled = preference?.EmailEnabled ?? false;
        var smsEnabled = preference?.SmsEnabled ?? false;

        var notification = new Notification
        {
            UserId = request.UserId,
            Title = title,
            Message = message,
            Type = string.IsNullOrWhiteSpace(request.Type) ? "Info" : request.Type.Trim(),
            Category = category,
            ActionUrl = string.IsNullOrWhiteSpace(request.ActionUrl) ? null : request.ActionUrl.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        db.Notifications.Add(notification);

        var dueAt = request.DeliverAtUtc?.ToUniversalTime() ?? DateTime.UtcNow;
        if (emailEnabled && !string.IsNullOrWhiteSpace(user.Email))
            notification.Deliveries.Add(new NotificationDelivery { Channel = "Email", Status = "Pending", NextAttemptAt = dueAt });
        if (smsEnabled && !string.IsNullOrWhiteSpace(user.PhoneNumber))
            notification.Deliveries.Add(new NotificationDelivery { Channel = "Sms", Status = "Pending", NextAttemptAt = dueAt });

        await db.SaveChangesAsync(ct);
        if (inAppEnabled)
        {
            await realtime.PublishAsync(request.UserId,
                new NotificationRealtimeMessage(notification.Id, notification.Title, notification.Message,
                    notification.Type, notification.Category, notification.ActionUrl, notification.CreatedAt), ct);
        }
        return Map(notification);
    }

    public async Task<NotificationPageResponse> GetMineAsync(string userId, int page, int pageSize, bool unreadOnly, CancellationToken ct = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 50);
        var query = db.Notifications.AsNoTracking().Where(x => x.UserId == userId);
        var total = await query.CountAsync(ct);
        var unread = await query.CountAsync(x => !x.IsRead, ct);
        if (unreadOnly) query = query.Where(x => !x.IsRead);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).Select(x => new NotificationResponse(
                x.Id, x.Title, x.Message, x.Type, x.Category, x.ActionUrl, x.IsRead, x.CreatedAt, x.ReadAt))
            .ToListAsync(ct);
        return new NotificationPageResponse(items, page, pageSize, unreadOnly ? unread : total, unread);
    }

    public Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default) =>
        db.Notifications.CountAsync(x => x.UserId == userId && !x.IsRead, ct);

    public async Task<bool> MarkAsReadAsync(string userId, long notificationId, CancellationToken ct = default)
    {
        var item = await db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, ct);
        if (item is null) return false;
        if (!item.IsRead) { item.IsRead = true; item.ReadAt = DateTime.UtcNow; await db.SaveChangesAsync(ct); }
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await db.Notifications.Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsRead, true).SetProperty(x => x.ReadAt, now), ct);
    }

    public async Task<bool> DeleteAsync(string userId, long notificationId, CancellationToken ct = default)
    {
        var item = await db.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId, ct);
        if (item is null) return false;
        db.Notifications.Remove(item); await db.SaveChangesAsync(ct); return true;
    }

    public async Task<IReadOnlyList<NotificationDeliveryStatusResponse>> GetDeliveryStatusAsync(string userId, long notificationId, CancellationToken ct = default)
    {
        return await db.NotificationDeliveries.AsNoTracking()
            .Where(x => x.NotificationId == notificationId && x.Notification.UserId == userId)
            .OrderBy(x => x.Channel)
            .Select(x => new NotificationDeliveryStatusResponse(x.Channel, x.Status, x.Attempts, x.NextAttemptAt, x.SentAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationPreferenceResponse>> GetPreferencesAsync(string userId, CancellationToken ct = default)
    {
        return await db.NotificationPreferences.AsNoTracking().Where(x => x.UserId == userId)
            .OrderBy(x => x.Category).Select(x => new NotificationPreferenceResponse(
                x.Category, x.InAppEnabled, x.EmailEnabled, x.SmsEnabled, x.UpdatedAt)).ToListAsync(ct);
    }

    public async Task<NotificationPreferenceResponse> UpdatePreferenceAsync(string userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        var category = string.IsNullOrWhiteSpace(request.Category) ? "*" : request.Category.Trim();
        if (category.Length > 80) throw new ArgumentException("Category must be at most 80 characters.");
        var row = await db.NotificationPreferences.FirstOrDefaultAsync(x => x.UserId == userId && x.Category == category, ct);
        if (row is null) { row = new NotificationPreference { UserId = userId, Category = category }; db.NotificationPreferences.Add(row); }
        row.InAppEnabled = request.InAppEnabled; row.EmailEnabled = request.EmailEnabled; row.SmsEnabled = request.SmsEnabled; row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new NotificationPreferenceResponse(row.Category, row.InAppEnabled, row.EmailEnabled, row.SmsEnabled, row.UpdatedAt);
    }

    private static NotificationResponse Map(Notification x) => new(x.Id, x.Title, x.Message, x.Type, x.Category, x.ActionUrl, x.IsRead, x.CreatedAt, x.ReadAt);
}

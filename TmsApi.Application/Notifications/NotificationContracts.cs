namespace TmsApi.Application.Notifications;

public sealed record CreateNotificationRequest(
    string UserId,
    string Title,
    string Message,
    string Type = "Info",
    string Category = "General",
    string? ActionUrl = null,
    DateTime? DeliverAtUtc = null);

public sealed record NotificationResponse(
    long Id, string Title, string Message, string Type, string Category,
    string? ActionUrl, bool IsRead, DateTime CreatedAt, DateTime? ReadAt);

public sealed record NotificationPageResponse(
    IReadOnlyList<NotificationResponse> Items, int Page, int PageSize, int TotalCount, int UnreadCount);

public sealed record NotificationDeliveryStatusResponse(string Channel, string Status, int Attempts, DateTime? NextAttemptAt, DateTime? SentAt);

public sealed record NotificationPreferenceResponse(
    string Category, bool InAppEnabled, bool EmailEnabled, bool SmsEnabled, DateTime UpdatedAt);

public sealed record UpdateNotificationPreferenceRequest(
    string Category, bool InAppEnabled, bool EmailEnabled, bool SmsEnabled);

public sealed record NotificationRealtimeMessage(
    long Id, string Title, string Message, string Type, string Category,
    string? ActionUrl, DateTime CreatedAt);

public interface INotificationService
{
    Task<NotificationResponse> CreateAsync(CreateNotificationRequest request, CancellationToken ct = default);
    Task<NotificationPageResponse> GetMineAsync(string userId, int page, int pageSize, bool unreadOnly, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(string userId, long notificationId, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(string userId, CancellationToken ct = default);
    Task<bool> DeleteAsync(string userId, long notificationId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationDeliveryStatusResponse>> GetDeliveryStatusAsync(string userId, long notificationId, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationPreferenceResponse>> GetPreferencesAsync(string userId, CancellationToken ct = default);
    Task<NotificationPreferenceResponse> UpdatePreferenceAsync(string userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default);
}

public interface INotificationRealtimePublisher
{
    Task PublishAsync(string userId, NotificationRealtimeMessage message, CancellationToken ct = default);
}

public interface INotificationChannelSender
{
    string Channel { get; }
    Task SendAsync(TmsApi.Domain.Entities.Notification notification, TmsApi.Domain.Entities.TmsUser user, CancellationToken ct);
}

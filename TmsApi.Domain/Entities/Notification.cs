namespace TmsApi.Domain.Entities;

public sealed class Notification
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info";
    public string Category { get; set; } = "General";
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public ICollection<NotificationDelivery> Deliveries { get; set; } = new List<NotificationDelivery>();
}

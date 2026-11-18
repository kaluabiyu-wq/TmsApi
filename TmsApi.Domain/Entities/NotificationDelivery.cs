namespace TmsApi.Domain.Entities;

public sealed class NotificationDelivery
{
    public long Id { get; set; }
    public long NotificationId { get; set; }
    public string Channel { get; set; } = "Email"; // Email | Sms
    public string Status { get; set; } = "Pending"; // Pending | Processing | Sent | Failed
    public int Attempts { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Notification Notification { get; set; } = null!;
}

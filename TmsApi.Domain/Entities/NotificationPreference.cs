namespace TmsApi.Domain.Entities;

/// <summary>One row per user and category. Use "*" as the user's default preference.</summary>
public sealed class NotificationPreference
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Category { get; set; } = "*";
    public bool InAppEnabled { get; set; } = true;
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

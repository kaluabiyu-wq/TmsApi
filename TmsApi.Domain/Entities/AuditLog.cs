namespace TmsApi.Domain.Entities;

/// <summary>Append-only institutional audit event. Old/NewValues contain redacted JSON.</summary>
public sealed class AuditLog
{
    public long Id { get; set; }
    public string UserId { get; set; } = "";
    public string Action { get; set; } = "";
    public string EntityName { get; set; } = "";
    public string EntityId { get; set; } = "";
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? CorrelationId { get; set; }
}

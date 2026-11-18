namespace TmsApi.Domain.Entities;

/// <summary>Tracks an exported report and its file metadata; report rows remain in source tables.</summary>
public sealed class GeneratedReport
{
    public long Id { get; set; }
    public int ReportDefinitionId { get; set; }
    public ReportDefinition ReportDefinition { get; set; } = null!;
    public required string RequestedByUserId { get; set; }
    public required string Format { get; set; }
    public required string Status { get; set; } = "Pending";
    public string FiltersJson { get; set; } = "{}";
    public string? FileName { get; set; }
    public string? StoragePath { get; set; }
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

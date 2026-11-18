namespace TmsApi.Domain.Entities;

/// <summary>Describes a report that the institution can generate. Report data is never copied here.</summary>
public sealed class ReportDefinition
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

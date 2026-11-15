namespace TmsApi.Domain.Entities;

/// <summary>
/// A physical or virtual room where training takes place.
/// Phase 1 uses it as the template entity; Phase 11 (scheduling) links sessions to it.
/// Rooms are never hard-deleted: a room that is no longer used is marked inactive,
/// so past schedules that point at it stay valid.
/// </summary>
public class Classroom
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string Location { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public bool IsActive { get; set; } = true;

    // Always UTC (see docs/API-STANDARDS.md).
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

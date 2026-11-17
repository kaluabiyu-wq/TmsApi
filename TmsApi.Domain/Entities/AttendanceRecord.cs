namespace TmsApi.Domain.Entities;

/// <summary>
/// One student's attendance for one session. (SessionId, StudentId) is unique,
/// so "marking" a student twice updates the same row instead of creating a duplicate.
/// </summary>
public class AttendanceRecord
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    // Student.ID (the legacy upper-case primary key name is kept as-is).
    public int StudentId { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;

    public DateTime? CheckInAt { get; set; }

    public string? Remarks { get; set; }

    // Identity user id of whoever saved the record last.
    public string RecordedBy { get; set; } = string.Empty;

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public TrainingSession Session { get; set; } = null!;

    public Student Student { get; set; } = null!;
}

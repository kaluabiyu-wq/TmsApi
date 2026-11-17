namespace TmsApi.Domain.Entities;

/// <summary>
/// One scheduled meeting of a course (a lecture, a lab, a workshop).
/// Attendance is always recorded against a session, never directly against a course.
/// Sessions are never hard-deleted: attendance history points at them.
/// </summary>
public class TrainingSession
{
    public int Id { get; set; }

    public int CourseId { get; set; }

    // Always UTC (see docs/API-STANDARDS.md).
    public DateTime StartAt { get; set; }

    public DateTime EndAt { get; set; }

    public string Topic { get; set; } = string.Empty;

    // Identity user id (TmsUser.Id) of the instructor who owns the session.
    public string InstructorId { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;

    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
}

namespace TmsApi.Domain.Entities;

/// <summary>Derived per-course progress snapshot. Completion is reconciled from the existing Enrollment record.</summary>
public sealed class StudentProgress
{
    public int Id { get; set; }
    public int StudentProgramEnrollmentId { get; set; }
    public StudentProgramEnrollment StudentProgramEnrollment { get; set; } = null!;
    public int ProgramCourseId { get; set; }
    public ProgramCourse ProgramCourse { get; set; } = null!;
    /// <summary>NotStarted, InProgress or Completed.</summary>
    public string Status { get; set; } = "NotStarted";
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}

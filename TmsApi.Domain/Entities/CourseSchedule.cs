namespace TmsApi.Domain.Entities;

/// <summary>
/// A concrete, dated booking for a course, instructor and classroom.
/// All timestamps are UTC instants.
/// </summary>
public class CourseSchedule
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public string InstructorId { get; set; } = string.Empty;
    public int ClassroomId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Topic { get; set; }
    public bool IsCancelled { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Course Course { get; set; } = null!;
    public TmsUser Instructor { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
}

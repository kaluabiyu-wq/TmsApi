using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.Scheduling;

public sealed record CreateCourseScheduleRequest
{
    [Range(1, int.MaxValue)] public int CourseId { get; init; }
    [Required, MinLength(1), MaxLength(450)] public string InstructorId { get; init; } = "";
    [Range(1, int.MaxValue)] public int ClassroomId { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    [MaxLength(300)] public string? Topic { get; init; }
}

public sealed record UpdateCourseScheduleRequest
{
    [Range(1, int.MaxValue)] public int CourseId { get; init; }
    [Required, MinLength(1), MaxLength(450)] public string InstructorId { get; init; } = "";
    [Range(1, int.MaxValue)] public int ClassroomId { get; init; }
    public DateTime StartAt { get; init; }
    public DateTime EndAt { get; init; }
    [MaxLength(300)] public string? Topic { get; init; }
    public bool IsCancelled { get; init; }
}

public sealed record CourseScheduleResponse(
    int Id,
    int CourseId,
    string CourseCode,
    string CourseTitle,
    string InstructorId,
    string InstructorName,
    int ClassroomId,
    string ClassroomName,
    string ClassroomLocation,
    DateTime StartAt,
    DateTime EndAt,
    string? Topic,
    bool IsCancelled);

public sealed record ScheduleCalendarQuery
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
    public string? InstructorId { get; init; }
    public int? ClassroomId { get; init; }
    public int? CourseId { get; init; }
}

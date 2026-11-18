namespace TmsApi.Application.Programs;

public sealed record CreateProgramRequest(string Code, string Name, string? Description, int? DurationMonths);
public sealed record AddProgramCourseRequest(int CourseId, int Sequence, bool IsRequired = true);
public sealed record RegisterStudentProgramRequest(int ProgramId, int StudentId);
public sealed record ProgramCourseProgressDto(int ProgramCourseId, int CourseId, string CourseCode, string CourseTitle, int Sequence, bool IsRequired, string Status, DateTime? StartedAt, DateTime? CompletedAt);
public sealed record StudentProgramProgressDto(int EnrollmentId, int StudentId, string StudentName, int ProgramId, string ProgramCode, string ProgramName, string EnrollmentStatus, int TotalCourses, int CompletedCourses, int RemainingCourses, decimal CompletionPercent, bool IsEligibleForCompletion, string? CurrentCourse, IReadOnlyList<ProgramCourseProgressDto> Courses);
public sealed record TrainingProgramDto(int Id, string Code, string Name, string Description, int? DurationMonths, bool IsActive, int CourseCount, int EnrolledStudents, IReadOnlyList<ProgramCourseProgressDto> Courses);

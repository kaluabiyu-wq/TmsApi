namespace TmsApi.Application.Scheduling;

public interface ICourseScheduleService
{
    Task<CourseScheduleResponse> CreateAsync(CreateCourseScheduleRequest request, CancellationToken ct);
    Task<CourseScheduleResponse?> UpdateAsync(int id, UpdateCourseScheduleRequest request, CancellationToken ct);
    Task<CourseScheduleResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<CourseScheduleResponse>> GetCalendarAsync(ScheduleCalendarQuery query, CancellationToken ct);
    Task<IReadOnlyList<CourseScheduleResponse>> GetInstructorScheduleAsync(string instructorId, DateTime from, DateTime to, CancellationToken ct);
    Task<IReadOnlyList<CourseScheduleResponse>> GetCourseScheduleAsync(int courseId, DateTime from, DateTime to, string? instructorId, CancellationToken ct);
}

using TmsApi.Application.DTOs;

namespace TmsApi.Application.Attendance;

/// <summary>Who is calling. The controller builds it from the JWT; the service never sees HttpContext.</summary>
public sealed record AttendanceActor(string UserId, bool IsAdmin);

/// <summary>
/// Attendance use cases. Conventions (docs/API-STANDARDS.md):
///   null return                      -> 404 (course / session / record / student does not exist)
///   InvalidOperationException        -> 409 (a business rule was broken)
///   UnauthorizedAccessException      -> 403 (an instructor touching a course they do not teach)
/// </summary>
public interface IAttendanceService
{
    Task<SessionResponse?> CreateSessionAsync(AttendanceActor actor, CreateSessionRequest request, CancellationToken ct);

    Task<PagedResponse<SessionResponse>?> GetCourseSessionsAsync(
        AttendanceActor actor, int courseId, int page, int pageSize, CancellationToken ct);

    Task<SessionAttendanceResponse?> GetSessionAttendanceAsync(AttendanceActor actor, int sessionId, CancellationToken ct);

    Task<SessionAttendanceResponse?> MarkAttendanceAsync(
        AttendanceActor actor, int sessionId, MarkAttendanceRequest request, CancellationToken ct);

    Task<AttendanceRecordResponse?> UpdateAttendanceAsync(
        AttendanceActor actor, int recordId, UpdateAttendanceRequest request, CancellationToken ct);

    Task<StudentAttendanceResponse?> GetStudentAttendanceAsync(
        AttendanceActor actor, int studentId, int? courseId, int page, int pageSize, CancellationToken ct);

    Task<CourseAttendanceSummaryResponse?> GetAttendanceSummaryAsync(AttendanceActor actor, int courseId, CancellationToken ct);
}

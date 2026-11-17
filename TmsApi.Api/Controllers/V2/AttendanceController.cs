using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Authorization;
using TmsApi.Application.Attendance;
using TmsApi.Application.DTOs;

namespace TmsApi.Api.Controllers.V2;


[ApiController]
[Route("api/v{version:apiVersion}")]
[ApiVersion("2.0")]
[Authorize(Policy = Policies.InstructorOrAdmin)]
public class AttendanceController(IAttendanceService attendance) : ControllerBase
{
    // ---- sessions

    [HttpPost("sessions")]
    public async Task<ActionResult<SessionResponse>> CreateSession(
        [FromBody] CreateSessionRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();

        var created = await attendance.CreateSessionAsync(actor, request, ct);
        return created is null
            ? NotFound()
            : CreatedAtAction(nameof(GetSessionAttendance), new { id = created.Id }, created);
    }

    [HttpGet("courses/{courseId:int}/sessions")]
    public async Task<ActionResult<PagedResponse<SessionResponse>>> GetCourseSessions(
        int courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();

        var result = await attendance.GetCourseSessionsAsync(actor, courseId, page, pageSize, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ---- attendance for one session

    /// <summary>The roster for a session: every approved student plus their current mark (if any).</summary>
    [HttpGet("sessions/{id:int}/attendance")]
    public async Task<ActionResult<SessionAttendanceResponse>> GetSessionAttendance(int id, CancellationToken ct)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();

        var result = await attendance.GetSessionAttendanceAsync(actor, id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    
    [HttpPost("sessions/{id:int}/attendance")]
    public async Task<ActionResult<SessionAttendanceResponse>> MarkAttendance(
        int id, [FromBody] MarkAttendanceRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();

        var result = await attendance.MarkAttendanceAsync(actor, id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("attendance/{id:int}")]
    public async Task<ActionResult<AttendanceRecordResponse>> UpdateAttendance(
        int id, [FromBody] UpdateAttendanceRequest request, CancellationToken ct)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();

        var result = await attendance.UpdateAttendanceAsync(actor, id, request, ct);
        return result is null ? NotFound() : Ok(result);
    }

    
    [HttpGet("students/{id:int}/attendance")]
    public async Task<ActionResult<StudentAttendanceResponse>> GetStudentAttendance(
        int id,
        [FromQuery] int? courseId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();

        var result = await attendance.GetStudentAttendanceAsync(actor, id, courseId, page, pageSize, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("courses/{courseId:int}/attendance/summary")]
    public async Task<ActionResult<CourseAttendanceSummaryResponse>> GetAttendanceSummary(
        int courseId, CancellationToken ct)
    {
        if (!TryGetActor(out var actor)) return Unauthorized();

        var result = await attendance.GetAttendanceSummaryAsync(actor, courseId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    private bool TryGetActor(out AttendanceActor actor)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        actor = new AttendanceActor(userId ?? string.Empty, User.IsInRole("Admin"));
        return !string.IsNullOrEmpty(userId);
    }
}

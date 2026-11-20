using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Authorization;
using TmsApi.Application.Scheduling;
using TmsApi.Api.Notifications;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}")]
[Authorize]
public sealed class SchedulesController(ICourseScheduleService schedules, RoleBasedNotificationService roleNotifications) : ControllerBase
{
    [HttpPost("schedules")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<CourseScheduleResponse>> Create(CreateCourseScheduleRequest request, CancellationToken ct)
    {
        try
        {
            var created = await schedules.CreateAsync(request, ct);
            await roleNotifications.NotifyScheduleChangedAsync(created, created: true, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id, version = "2.0" }, created);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("schedules/{id:int}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<CourseScheduleResponse>> Update(int id, UpdateCourseScheduleRequest request, CancellationToken ct)
    {
        try
        {
            var previous = await schedules.GetByIdAsync(id, ct);
            var result = await schedules.UpdateAsync(id, request, ct);
            if (result is null) return NotFound();
            if (previous is not null && !string.IsNullOrWhiteSpace(previous.InstructorId) &&
                !string.Equals(previous.InstructorId, result.InstructorId, StringComparison.Ordinal))
            {
                await roleNotifications.NotifyUsersAsync([previous.InstructorId],
                    "Class assignment changed",
                    $"You are no longer assigned to the class for {result.CourseTitle} on {result.StartAt:g}.",
                    "Timetable", "Warning", "/instructor/timetable", ct);
            }
            await roleNotifications.NotifyScheduleChangedAsync(result, created: false, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("schedules/{id:int}")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<CourseScheduleResponse>> GetById(int id, CancellationToken ct)
    {
        var item = await schedules.GetByIdAsync(id, ct);
        if (item is null) return NotFound();
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin") &&
            item.InstructorId != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        return Ok(item);
    }

    [HttpGet("schedules/calendar")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<IReadOnlyList<CourseScheduleResponse>>> Calendar(
        [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to,
        [FromQuery] string? instructorId, [FromQuery] int? classroomId, [FromQuery] int? courseId,
        CancellationToken ct)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
            instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try { return Ok(await schedules.GetCalendarAsync(new ScheduleCalendarQuery
            { From = from.UtcDateTime, To = to.UtcDateTime, InstructorId = instructorId, ClassroomId = classroomId, CourseId = courseId }, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("instructors/{id}/schedule")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<IReadOnlyList<CourseScheduleResponse>>> InstructorSchedule(
        string id, [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken ct)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin") &&
            id != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        try { return Ok(await schedules.GetInstructorScheduleAsync(id, from.UtcDateTime, to.UtcDateTime, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("courses/{id:int}/schedule")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<IReadOnlyList<CourseScheduleResponse>>> CourseSchedule(
        int id, [FromQuery] DateTimeOffset from, [FromQuery] DateTimeOffset to, CancellationToken ct)
    {
        try { var instructorId = User.IsInRole("Instructor") && !User.IsInRole("Admin")
                ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            return Ok(await schedules.GetCourseScheduleAsync(id, from.UtcDateTime, to.UtcDateTime, instructorId, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

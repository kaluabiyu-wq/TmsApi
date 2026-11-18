using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Authorization;
using TmsApi.Application.Scheduling;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}")]
[Authorize]
public sealed class SchedulesController(ICourseScheduleService schedules) : ControllerBase
{
    [HttpPost("schedules")]
    [Authorize(Policy = Policies.AdminOnly)]
    public async Task<ActionResult<CourseScheduleResponse>> Create(CreateCourseScheduleRequest request, CancellationToken ct)
    {
        try
        {
            var created = await schedules.CreateAsync(request, ct);
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
            var result = await schedules.UpdateAsync(id, request, ct);
            return result is null ? NotFound() : Ok(result);
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
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] string? instructorId, [FromQuery] int? classroomId, [FromQuery] int? courseId,
        CancellationToken ct)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin"))
            instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        try { return Ok(await schedules.GetCalendarAsync(new ScheduleCalendarQuery
            { From = from, To = to, InstructorId = instructorId, ClassroomId = classroomId, CourseId = courseId }, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("instructors/{id}/schedule")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<IReadOnlyList<CourseScheduleResponse>>> InstructorSchedule(
        string id, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        if (User.IsInRole("Instructor") && !User.IsInRole("Admin") &&
            id != User.FindFirstValue(ClaimTypes.NameIdentifier)) return Forbid();
        try { return Ok(await schedules.GetInstructorScheduleAsync(id, from, to, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("courses/{id:int}/schedule")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<IReadOnlyList<CourseScheduleResponse>>> CourseSchedule(
        int id, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
    {
        try { var instructorId = User.IsInRole("Instructor") && !User.IsInRole("Admin")
                ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
            return Ok(await schedules.GetCourseScheduleAsync(id, from, to, instructorId, ct)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}

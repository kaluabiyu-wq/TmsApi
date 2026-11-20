using System.Security.Claims;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Hubs;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Application.Hubs;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Notifications;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("2.0")]
public class EnrollmentsController(TmsDbContext context, IMediator mediator, IHubContext<TmsHub, ITmsHubClient> hubContext, IAuditService audit, TmsApi.Api.Notifications.RoleBasedNotificationService roleNotifications) : ControllerBase
{
    [Authorize(Roles = "Student")]
    [HttpPost]
    public async Task<IActionResult> Enroll(EnrollstudentCommand command, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var studentId = await context.Students.AsNoTracking()
            .Where(x => x.UserId == userId && x.IsActive)
            .Select(x => (int?)x.ID).FirstOrDefaultAsync(ct);
        if (studentId is null) return Forbid();

        // Ignore any client-supplied StudentId; the authenticated account determines the recipient.
        command = command with { StudentId = studentId.Value };
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess)
            await roleNotifications.NotifyEnrollmentSubmittedAsync(result.Value.EnrollmentId, ct);

        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(
                nameof(GetSchedule),
                new { studentId = created.StudentId },
                created),
            onFailure: error =>
            {
                var status = error.Code switch
                {
                    "course_not_found" => StatusCodes.Status404NotFound,
                    "course_full" or "already_enrolled" => StatusCodes.Status409Conflict,
                    _ => StatusCodes.Status400BadRequest
                };

                return Problem(
                    statusCode: status,
                    title: "Enrollment rejected",
                    detail: error.Message,
                    type: $"https://tms.local/errors/{error.Code}");
            });
    }


// Approving enrollments is a staff decision: self-registered Students must not do it.
[Authorize(Roles = "Instructor,Admin")]
[HttpPost("{id:int}/approve")]
[Route("~/api/v2/enrollments/{id:int}/approve")]
public async Task<IActionResult> Approve(int id, CancellationToken ct)
{
    var enrollment = await context.Enrollments
        .Include(e => e.Course)
        .Include(e => e.Student)
        .FirstOrDefaultAsync(e => e.ID == id, ct);

    if (enrollment is null)
        return NotFound(new { message = "Enrollment not found." });

    if (User.IsInRole("Instructor") && !User.IsInRole("Admin") &&
       enrollment.Course.InstructorId !=
    (User.FindFirstValue(ClaimTypes.NameIdentifier)
     ?? User.FindFirstValue("sub")))
        return Forbid();

    if (enrollment.Status != "Pending")
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Enrollment rejected",
            detail: $"Enrollment {id} is already '{enrollment.Status}' and cannot be re-approved.",
            type: "https://tms.local/errors/invalid_status_transition");
    }

     var approvedCount = await context.Enrollments
        .CountAsync(e => e.CourseId == enrollment.CourseId && e.Status == "Approved", ct);

    if (approvedCount >= enrollment.Course.MaxCapacity)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Enrollment rejected",
            detail: $"Course {enrollment.CourseId} is full ({approvedCount}/{enrollment.Course.MaxCapacity}).",
            type: "https://tms.local/errors/course_full");
    }

    var oldValues = new { enrollment.Status };
    enrollment.Status = "Approved";

    try
    {
        await context.SaveChangesAsync(ct);
    }
    catch (DbUpdateConcurrencyException)
    {
        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Enrollment rejected",
            detail: "This enrollment was modified by another request. Reload and try again.",
            type: "https://tms.local/errors/concurrency_conflict");
    }

    await audit.LogApprovalAsync("Enrollment", id.ToString(), oldValues, new { enrollment.Status, enrollment.CourseId, enrollment.StudentId }, ct);

    await roleNotifications.NotifyEnrollmentDecisionAsync(id, "Approved", ct);
    if (!string.IsNullOrWhiteSpace(enrollment.Student.UserId))
        await hubContext.Clients.User(enrollment.Student.UserId).ReceiveEnrollmentStatusUpdated(id, "Approved");

    return NoContent();
}






[Authorize(Roles = "Instructor,Admin")]
[HttpPost("{id:int}/reject")]
[Route("~/api/v2/enrollments/{id:int}/reject")]
public async Task<IActionResult> Reject(int id, CancellationToken ct)
{
    var enrollment = await context.Enrollments
        .Include(e => e.Course)
        .Include(e => e.Student)
        .FirstOrDefaultAsync(e => e.ID == id, ct);

    if (enrollment is null) return NotFound();
    if (User.IsInRole("Instructor") && !User.IsInRole("Admin") &&
       enrollment.Course.InstructorId !=
    (User.FindFirstValue(ClaimTypes.NameIdentifier)
     ?? User.FindFirstValue("sub")))
        return Forbid();
    if (enrollment.Status != "Pending")
        return Conflict(new { message = $"Enrollment {id} is already '{enrollment.Status}'." });

    enrollment.Status = "Rejected";
    try { await context.SaveChangesAsync(ct); }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict(new { message = "This enrollment was modified by another request. Reload and try again." });
    }

    await roleNotifications.NotifyEnrollmentDecisionAsync(id, "Rejected", ct);
    if (!string.IsNullOrWhiteSpace(enrollment.Student.UserId))
        await hubContext.Clients.User(enrollment.Student.UserId).ReceiveEnrollmentStatusUpdated(id, "Rejected");
    return NoContent();
}

    [HttpGet("{studentId}/schedule")]
    public async Task<IActionResult> GetSchedule(int studentId, CancellationToken ct)
    {
        var schedule = await mediator.Send(new GetStudentScheduleQuery(studentId), ct);
        return Ok(schedule);
    }
}
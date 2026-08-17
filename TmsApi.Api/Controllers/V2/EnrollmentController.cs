using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Hubs;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Application.Hubs;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("2.0")]
public class EnrollmentsController(TmSDbContext context,IMediator mediator,IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Enroll(EnrollstudentCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);

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


[HttpPost("{id:int}/approve")]
[Route("~/api/v2/enrollments/{id:int}/approve")]
public async Task<IActionResult> Approve(int id, CancellationToken ct)
{
    var enrollment = await context.Enrollments
        .Include(e => e.Course)
        .FirstOrDefaultAsync(e => e.ID == id, ct);

    if (enrollment is null)
        return NotFound(new { message = "Enrollment not found." });

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

    await hubContext.Clients.All.ReceiveEnrollmentStatusUpdated(id, "Approved");

    return NoContent();
}




    [HttpGet("{studentId}/schedule")]
    public async Task<IActionResult> GetSchedule(int studentId, CancellationToken ct)
    {
        var schedule = await mediator.Send(new GetStudentScheduleQuery(studentId), ct);
        return Ok(schedule);
    }
}
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Hubs;
using TmsApi.Application.Hubs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

public record CreateEnrollmentRequest(string StudentId);

public record EnrollmentSummaryDto(
    int Id,
    string StudentName,
    string CourseName,
    string Status,
    DateTime EnrolledAt);

[ApiController]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/enrollments")]
[ApiVersion("1.0")]
public class EnrollmentController(TmsDbContext context,IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEnrollments(
        int courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = context.Enrollments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId);

        var totalCount = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(a => a.StudentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.ID,
                a.CourseId,
                a.StudentId,
                a.EnrolledAt,
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize,
            totalPages,
            hasNext = page < totalPages,
            hasPrevious = page > 1
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        int courseId,
        [FromBody] CreateEnrollmentRequest request,
        CancellationToken ct)
    {
        var course = await context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

        if (course is null)
        {
            return NotFound(new { message = $"Course {courseId} was not found." });
        }

        var student = await context.Students
            .FirstOrDefaultAsync(s => s.RegistrationNumber == request.StudentId, ct);

        if (student is null)
        {
            return NotFound(new
            {
                message = $"No student found with registration number '{request.StudentId}'.",
            });
        }

        var alreadyEnrolled = await context.Enrollments
            .AnyAsync(e => e.CourseId == courseId && e.StudentId == student.ID, ct);

        if (alreadyEnrolled)
        {
            return Conflict(new { message = "This student is already enrolled in this course." });
        }

        var currentEnrollmentCount = await context.Enrollments
            .CountAsync(e => e.CourseId == courseId, ct);

        if (currentEnrollmentCount >= course.MaxCapacity)
        {
            return Conflict(new { message = "This course is full." });
        }

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = student.ID,
            EnrolledAt = DateTime.UtcNow,
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetEnrollments),
            new { courseId, version = "1.0" },
            new
            {
                enrollment.ID,
                enrollment.CourseId,
                StudentId = student.RegistrationNumber,
                enrollment.EnrolledAt,
            });
    }

  
  
    [HttpGet]
    [Route("~/api/v1/enrollments")]
    public async Task<IActionResult> GetAllEnrollments(CancellationToken ct)
    {
        var items = await context.Enrollments
            .AsNoTracking()
            .Select(e => new EnrollmentSummaryDto(
                e.ID,
                e.Student.Name,
                e.Course.Title,
                e.Status,
                e.EnrolledAt))
            .ToListAsync(ct);

        return Ok(items);
    }

   
[HttpPost("~/api/v1/enrollments/{id:int}/approve")]
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

}
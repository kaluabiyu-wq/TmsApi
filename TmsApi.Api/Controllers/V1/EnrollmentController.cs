using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

public record CreateEnrollmentRequest(string StudentId);

[ApiController]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/enrollments")]
[ApiVersion("1.0")]
public class EnrollmentController(TmSDbContext context) : ControllerBase
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
}
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/enrollments")]
[ApiVersion("2.0")]
public class EnrollmentController(TmSDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

          var courseExists = await context.Courses.AsNoTracking()
            .AnyAsync(c => c.Id == courseId, ct);

        if (!courseExists)
        {
            return NotFound(new { Title = $"Course {courseId} was not found." });
        }

        var baseQuery = context.Enrollments.AsNoTracking();
        var totalCount = await baseQuery.CountAsync(ct);

        var rows = await baseQuery
            .OrderBy(c => c.StudentId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.ID,
                c.CourseId,
                c.StudentId,
                c.EnrolledAt
               
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var hasNext = page < totalPages;
        var hasPrevious = page > 1;

         return Ok(new
        {
            data = rows,
            meta = new
            {
                totalCount,
                page,
                pageSize,
                totalPages,
                hasNext,
                hasPrevious
            },
            links = new
        {
              self = $"/api/v2/courses/{courseId}/enrollments?page={page}&pageSize={pageSize}",
                next = hasNext
                    ? $"/api/v2/courses/{courseId}/enrollments?page={page + 1}&pageSize={pageSize}"
                    : (string?)null,
                prev = hasPrevious
                    ? $"/api/v2/courses/{courseId}/enrollments?page={page - 1}&pageSize={pageSize}"
                    : (string?)null,
                course = $"/api/v2/courses/{courseId}"
        }

    });
    }
}
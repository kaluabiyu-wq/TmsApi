using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TmsApi.Api.Authorization;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/assessments")]
[ApiVersion("2.0")]
public class AssessmentController(TmsDbContext context, IAuthorizationService authorization) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAssessments(
        int courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var course = await context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null) return NotFound();
        var instructorAccess = await authorization.AuthorizeAsync(User, course, new CourseInstructorRequirement());
        var query = context.Assessments.AsNoTracking().Where(a => a.CourseId == courseId);
        if (!instructorAccess.Succeeded)
        {
            if (!User.IsInRole("Student")) return Forbid();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var studentId = await context.Students.AsNoTracking().Where(s => s.UserId == userId && s.IsActive).Select(s => (int?)s.ID).FirstOrDefaultAsync(ct);
            if (studentId is null || !await context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId && e.Status == "Approved", ct))
                return Forbid();
            query = query.Where(a => a.Status == "Published");
        }

        var baseQuery = query
            .AsNoTracking()
            .Where(a => a.CourseId == courseId);

        var totalCount = await baseQuery.CountAsync(ct);

        var rows = await baseQuery
            .OrderBy(a => a.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Weight,
                a.MaxScore,
                a.CourseId,
                a.Description,
                a.OpensAt,
                a.ClosesAt,
                a.DurationMinutes,
                a.MaxAttempts,
                a.Status
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
                self = $"/api/v2/courses/{courseId}/assessments?page={page}&pageSize={pageSize}",
                next = hasNext
                    ? $"/api/v2/courses/{courseId}/assessments?page={page + 1}&pageSize={pageSize}"
                    : (string?)null,
                prev = hasPrevious
                    ? $"/api/v2/courses/{courseId}/assessments?page={page - 1}&pageSize={pageSize}"
                    : (string?)null,
                course = $"/api/v2/courses/{courseId}"
            }
        });
    }
}
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/assessments")]
[ApiVersion("2.0")]
public class AssessmentController(TmSDbContext context) : ControllerBase
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

        var baseQuery = context.Assessments
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
                a.CourseId
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
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/certficates")]
[ApiVersion("2.0")]


public class CertficateController(TmSDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCertficates(
            int courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default
    )
    {
        page = Math.Max(1,page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = context.Certificates
        .AsNoTracking()
        .Where(a=>a.CourseId == courseId);

       var totalCount = await baseQuery.CountAsync(ct);

        var rows = await baseQuery
            .OrderBy(a => a.SerialNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.SerialNumber,
                a.IssuedAt,
                a.StudentId,
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
              self = $"/api/v2/courses/{courseId}/certficates?page={page}&pageSize={pageSize}",
                next = hasNext
                    ? $"/api/v2/courses/{courseId}/certficates?page={page + 1}&pageSize={pageSize}"
                    : (string?)null,
                prev = hasPrevious
                    ? $"/api/v2/courses/{courseId}/certficates?page={page - 1}&pageSize={pageSize}"
                    : (string?)null,
                course = $"/api/v2/courses/{courseId}"
        }

    });


    }

}
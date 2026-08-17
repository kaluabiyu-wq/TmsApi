using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

public record CreateGradeRequest(int StudentId, int CourseId, int Score);

[ApiController]
[Route("api/v{version:apiVersion}/grades")]
[ApiVersion("1.0")]
public class GradeController(TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var grades = await context.Grades
            .AsNoTracking()
            .Select(g => new { g.Id, g.StudentId, g.CourseId, g.Score})
            .ToListAsync(ct);

        return Ok(grades);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateGradeRequest request,
        CancellationToken ct)
    {
        if (request.Score < 0 || request.Score > 100)
        {
            return BadRequest(new
            {
                message = "Score must be between 0 and 100.",
            });
        }

        var studentExists = await context.Students
            .AnyAsync(s => s.ID == request.StudentId, ct);

        if (!studentExists)
        {
            return NotFound(new
            {
                message = $"Student {request.StudentId} not found.",
            });
        }

        var courseExists = await context.Courses
            .AnyAsync(c => c.Id == request.CourseId, ct);

        if (!courseExists)
        {
            return NotFound(new
            {
                message = $"Course {request.CourseId} not found.",
            });
        }

        var grade = new Grade
        {
          
            StudentId = request.StudentId,
            CourseId = request.CourseId,
            Score = request.Score,
          
        };

        context.Grades.Add(grade);
        await context.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetAll),
            new { version = "1.0" },
            new { id = grade.Id, success = true });
    }
}
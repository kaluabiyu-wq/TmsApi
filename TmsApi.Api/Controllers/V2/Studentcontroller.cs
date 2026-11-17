using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/students")]
[ApiVersion("2.0")]
public class StudentController(TmsDbContext context, UserManager<TmsUser> userManager) : ControllerBase
{

    public sealed record LinkStudentAccountRequest(string UserId);

    [Authorize(Roles = "Admin")]
    [HttpPut("{studentId:int}/link-account")]
    public async Task<IActionResult> LinkAccount(int studentId, [FromBody] LinkStudentAccountRequest request, CancellationToken ct)
    {
        var student = await context.Students.FirstOrDefaultAsync(s => s.ID == studentId, ct);
        if (student is null) return NotFound();
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user is null) return BadRequest(new { message = "Identity user was not found." });
        var alreadyLinked = await context.Students.AnyAsync(s => s.ID != studentId && s.UserId == request.UserId, ct);
        if (alreadyLinked) return Conflict(new { message = "This account is already linked to another student record." });
        student.UserId = request.UserId;
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetStudents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var baseQuery = context.Students.AsNoTracking();

        var totalCount = await baseQuery.CountAsync(ct);

        var rows = await baseQuery
            .OrderBy(s => s.ID)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.ID,
                s.RegistrationNumber,
                s.Name,
                s.GPA,
                s.IsActive
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
                self = $"/api/v2/students?page={page}&pageSize={pageSize}",
                next = hasNext
                    ? $"/api/v2/students?page={page + 1}&pageSize={pageSize}"
                    : (string?)null,
                prev = hasPrevious
                    ? $"/api/v2/students?page={page - 1}&pageSize={pageSize}"
                    : (string?)null,
                course = "/api/v2/students"
            }
        });
    }
}
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

    public sealed record UpdateStudentStatusRequest(string Status);

    [Authorize(Roles = "Admin,SuperAdmin,Registrar")]
    [HttpPut("{studentId:int}/status")]
    public async Task<IActionResult> UpdateStatus(int studentId, [FromBody] UpdateStudentStatusRequest request, CancellationToken ct)
    {
        var normalized = request.Status?.Trim();
        if (normalized is not ("Active" or "Inactive" or "Graduated"))
            return BadRequest(new { message = "Status must be Active, Inactive or Graduated." });
        var student = await context.Students.FirstOrDefaultAsync(x => x.ID == studentId, ct);
        if (student is null) return NotFound();
        student.Status = normalized;
        student.IsActive = normalized == "Active";
        await context.SaveChangesAsync(ct);
        return Ok(new { student.ID, student.Status, student.IsActive });
    }

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
                s.IsActive,
                s.Status
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
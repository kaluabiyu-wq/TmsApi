using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Programs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/programs")]
[Produces("application/json")]
public sealed class ProgramsController(IProgramService programs, IProgressService progress) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin,Registrar,Instructor,Student,Auditor")]
    public async Task<IActionResult> Get([FromQuery] bool includeInactive = false, CancellationToken ct = default)
        => Ok(await programs.GetProgramsAsync(includeInactive && (User.IsInRole("Admin") || User.IsInRole("SuperAdmin")), ct));

    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateProgramRequest request, CancellationToken ct)
    {
        try { var result = await programs.CreateProgramAsync(request, ct); return Created("/api/v2/programs", result); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("{id:int}/courses")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> AddCourse(int id, [FromBody] AddProgramCourseRequest request, CancellationToken ct)
    {
        try { var result = await programs.AddCourseToProgramAsync(id, request, ct); return result is null ? NotFound() : Ok(result); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}/progress")]
    [Authorize(Roles = "Admin,SuperAdmin,Registrar,Auditor")]
    public async Task<IActionResult> ProgramProgress(int id, CancellationToken ct)
    {
        var result = await progress.GetProgramProgressAsync(id, ct);
        return Ok(result);
    }
}

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/students/{studentId:int}/programs")]
[Produces("application/json")]
public sealed class StudentProgramsController(IProgramService programs, IProgressService progress, TmsDbContext db) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Admin,SuperAdmin,Registrar")]
    public async Task<IActionResult> Register(int studentId, [FromBody] RegisterStudentProgramRequest request, CancellationToken ct)
    {
        if (studentId != request.StudentId) return BadRequest(new { message = "The route studentId must match the request studentId." });
        try { return Ok(await programs.RegisterStudentAsync(request.ProgramId, studentId, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("~/api/v{version:apiVersion}/students/{studentId:int}/progress")]
    [Authorize(Roles = "Admin,SuperAdmin,Registrar,Student")]
    public async Task<IActionResult> GetProgress(int studentId, CancellationToken ct)
    {
        if (User.IsInRole("Student"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            var ownsRecord = await db.Students.AnyAsync(x => x.ID == studentId && x.UserId == userId && !x.IsDeleted, ct);
            if (!ownsRecord) return Forbid();
        }
        return Ok(await progress.GetStudentProgressAsync(studentId, ct));
    }

    [HttpGet("~/api/v{version:apiVersion}/students/me/progress")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyProgress(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var studentId = await db.Students.AsNoTracking().Where(x => x.UserId == userId && !x.IsDeleted).Select(x => (int?)x.ID).FirstOrDefaultAsync(ct);
        return studentId is null ? NotFound(new { message = "No student profile is linked to this account." }) : Ok(await progress.GetStudentProgressAsync(studentId.Value, ct));
    }

    [HttpPost("{enrollmentId:int}/recalculate")]
    [Authorize(Roles = "Admin,SuperAdmin,Registrar")]
    public async Task<IActionResult> Recalculate(int studentId, int enrollmentId, CancellationToken ct)
    {
        var record = await db.StudentProgramEnrollments.AsNoTracking().AnyAsync(x => x.Id == enrollmentId && x.StudentId == studentId, ct);
        if (!record) return NotFound();
        var result = await progress.RecalculateProgressAsync(enrollmentId, ct);
        return result is null ? NotFound() : Ok(result);
    }

}

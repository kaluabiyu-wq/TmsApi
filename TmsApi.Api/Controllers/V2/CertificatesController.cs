using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.Certificates;
using TmsApi.Application.Notifications;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/certificates")]
[Authorize]
public sealed class CertificatesController(ICertificateIssuanceService certificates, TmsDbContext db, INotificationService notifications) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
    private string? ClientIp => HttpContext.Connection.RemoteIpAddress?.ToString();
    private bool IsStaff => User.IsInRole("Admin") || User.IsInRole("Registrar") || User.IsInRole("SuperAdmin");

    [HttpPost("issue")]
    [Authorize(Roles = "Admin,Registrar,SuperAdmin")]
    public async Task<IActionResult> Issue([FromBody] IssueCertificateRequest request, CancellationToken ct)
    {
        try
        {
            var result = await certificates.IssueAsync(
                request.StudentId, request.CourseId, request.TemplateId, UserId, ClientIp, ct);
            var recipient = await db.Students.AsNoTracking().Where(s => s.ID == request.StudentId)
                .Select(s => s.UserId).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrWhiteSpace(recipient))
            {
                await notifications.CreateAsync(new CreateNotificationRequest(
                    recipient, "Certificate issued", "Your training certificate is ready to view and download.",
                    "Success", "Certificates", "/my-certificates"), ct);
            }
            return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "2" }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Registrar,SuperAdmin")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await certificates.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Registrar,SuperAdmin")]
    public async Task<IActionResult> GetAll([FromQuery] int take = 100, CancellationToken ct = default) =>
        Ok(await certificates.GetAllAsync(take, ct));

    [HttpGet("student/me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyCertificates(CancellationToken ct)
    {
        var ownId = await db.Students.AsNoTracking()
            .Where(s => s.UserId == UserId && s.IsActive)
            .Select(s => (int?)s.ID)
            .SingleOrDefaultAsync(ct);

        if (ownId is null) return Forbid();
        return Ok(await certificates.GetForStudentAsync(ownId.Value, ct));
    }

    [HttpGet("student/{id:int}")]
    public async Task<IActionResult> GetForStudent(int id, CancellationToken ct)
    {
        if (!IsStaff)
        {
            var ownId = await db.Students.AsNoTracking()
                .Where(s => s.UserId == UserId && s.IsActive)
                .Select(s => (int?)s.ID)
                .SingleOrDefaultAsync(ct);
            if (ownId is null || ownId.Value != id) return Forbid();
        }
        return Ok(await certificates.GetForStudentAsync(id, ct));
    }

    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length > 100) return BadRequest();
        return Ok(await certificates.VerifyAsync(code, ClientIp, ct));
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id, CancellationToken ct)
    {
        try
        {
            var result = await certificates.DownloadAsync(id, UserId, IsStaff, ClientIp, ct);
            return result is null ? NotFound() :
                File(result.Value.Content, "application/pdf", result.Value.FileName);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("{id:int}/revoke")]
    [Authorize(Roles = "Admin,Registrar,SuperAdmin")]
    public async Task<IActionResult> Revoke(
        int id, [FromBody] RevokeCertificateRequest request, CancellationToken ct)
    {
        var revoked = await certificates.RevokeAsync(id, request.Reason, UserId, ClientIp, ct);
        return revoked ? NoContent() : NotFound();
    }
}

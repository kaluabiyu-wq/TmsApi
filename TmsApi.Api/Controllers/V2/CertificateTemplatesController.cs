using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Certificates;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/certificate-templates")]
[Authorize(Roles = "Admin,Registrar,SuperAdmin")]
public sealed class CertificateTemplatesController(ICertificateTemplateService templates) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await templates.GetAllAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] SaveCertificateTemplateRequest request, CancellationToken ct)
    {
        try
        {
            var result = await templates.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetAll), new { version = "2" }, result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id, [FromBody] SaveCertificateTemplateRequest request, CancellationToken ct)
    {
        try
        {
            var result = await templates.UpdateAsync(id, request, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

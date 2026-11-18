using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.Reports;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Roles="Admin,SuperAdmin,Registrar,Auditor")]
[Produces("application/json")]
public sealed class ReportsController(IInstitutionalReportService reports) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
    private bool CanViewAll => User.IsInRole("Admin") || User.IsInRole("SuperAdmin") || User.IsInRole("Registrar") || User.IsInRole("Auditor");

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DateTime? from,[FromQuery] DateTime? to,[FromQuery] decimal passMark=50,CancellationToken ct=default)
    {
        if(!ValidDates(from,to)) return BadRequest(new{message="From date must be on or before To date."});
        try { return Ok(await reports.GetDashboardSummaryAsync(new ReportQuery(from,to,PassMark:passMark),ct)); }
        catch(ArgumentException ex) { return BadRequest(new{message=ex.Message}); }
    }

    [HttpGet("{reportType}")]
    public async Task<IActionResult> GetReport(string reportType,[FromQuery] DateTime? from,[FromQuery] DateTime? to,[FromQuery] int page=1,[FromQuery] int pageSize=25,[FromQuery] string? search=null,[FromQuery] int? courseId=null,[FromQuery] decimal passMark=50,CancellationToken ct=default)
    {
        if(!ValidDates(from,to)) return BadRequest(new{message="From date must be on or before To date."});
        try{return Ok(await reports.GetReportAsync(reportType,new ReportQuery(from,to,page,pageSize,search,courseId,passMark),ct));}
        catch(ArgumentException ex){return BadRequest(new{message=ex.Message});}
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateReportRequest request,CancellationToken ct)
    {
        var userId=UserId; if(string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        try { var result=await reports.GenerateAsync(userId,request,ct); return result.Status=="Failed"?UnprocessableEntity(result):AcceptedAtAction(nameof(Status),new{id=result.Id},result); }
        catch(ArgumentException ex){return BadRequest(new{message=ex.Message});}
        catch(InvalidOperationException ex){return BadRequest(new{message=ex.Message});}
    }

    [HttpGet("{id:long}/status")]
    public async Task<IActionResult> Status(long id,CancellationToken ct)
    { var userId=UserId; if(string.IsNullOrWhiteSpace(userId)) return Unauthorized(); var result=await reports.GetGeneratedAsync(id,userId,CanViewAll,ct); return result is null?NotFound():Ok(result); }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id,CancellationToken ct)
    { var userId=UserId; if(string.IsNullOrWhiteSpace(userId)) return Unauthorized(); var result=await reports.DownloadAsync(id,userId,CanViewAll,ct); return result is null?NotFound(new{message="Report not found, not ready, or expired."}):File(result.Value.Content,result.Value.ContentType,result.Value.FileName); }

    private static bool ValidDates(DateTime? from,DateTime? to)=>!from.HasValue||!to.HasValue||from.Value.Date<=to.Value.Date;
}

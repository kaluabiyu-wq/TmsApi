using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;



[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController(IEnrollmentService enrollmentService,TmSDbContext context):ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var enrollments = await enrollmentService.GetAllAsync();
        return Ok(enrollments);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var record = await enrollmentService.GetByIdAsync(id);
        return record is not null ? Ok(record):NotFound();
    }

    [HttpPost]
    public async Task<IActionResult>Create([FromBody] CreateEnrollmentRequest request)
    {
        var record = await enrollmentService.EnrollAsync(request.StudentId, request.CourseCode);
        return CreatedAtAction(nameof(GetById), new {id = record.Id},record);

    }
    [HttpDelete("{id}")]
    public async Task<IActionResult>Delete(String id)
    {
        var deleted = await enrollmentService.DeleteAsync(id);
        return deleted ? NoContent():NotFound();
    }

 [HttpPost("archive")]
    public async Task<IActionResult> BulkArchiveEnrollmentsAsync(  
        int cutoffYear, CancellationToken cancellationToken)
    {
        if (cutoffYear >= DateTime.UtcNow.Year)
            return BadRequest("cutoffYear must be before the current year.");

        int rowsAffected = await context.Enrollments
            .Where(e => e.Year <= cutoffYear && !e.IsArchived)
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.IsArchived, true),
                cancellationToken);

        return Ok(new { archived = rowsAffected, cutoffYear });
    }
}


 public record CreateEnrollmentRequest(string StudentId,string CourseCode);


 
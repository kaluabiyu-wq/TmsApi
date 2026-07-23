using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

[ApiController]
[Route("api/v2/transcripts")]
public class TranscriptsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public IActionResult RequestTranscipt([FromBody] object? _)
    {
        return Ok();
    }
[HttpGet("search")]
[EnableRateLimiting("search")]
public async Task<IActionResult> SearchCourses(
    [FromQuery] string? term, 
    CancellationToken ct)
{
        var results = await mediator.Send(new SearchCoursesQuery(term), ct);
        return Ok(results);

}

public record SearchCoursesQuery(string term);

}
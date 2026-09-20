using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

[ApiController]
[Route("api/v2/transcripts")]
[ApiVersion("2.0")]
public class SearchController(IMediator mediator) : ControllerBase
{
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
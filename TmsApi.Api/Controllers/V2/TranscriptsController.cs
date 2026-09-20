using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Asp.Versioning;

[ApiController]
[Route("api/v2/transcripts")]
[ApiVersion("2.0")]

public class TranscriptsController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public IActionResult RequestTranscipt([FromBody] object? _)
    {
        return Ok();
    }

}
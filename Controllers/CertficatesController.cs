using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tms.Api.Dtos;
using Tms.Api.Services;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/certficates")]
[Tags("Certficates")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status500InternalServerError)]

public class CertficateController(
ICourseService courseService, ICertficateService certficateService
) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetCertficate))]
    [ProducesResponseType(typeof(CertficateResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one certficate for a course")]

    public async Task<IActionResult> GetCertficate(int courseId, int id, CancellationToken ct)
    {
         var certifcate = await certficateService.GetByIdAsync(courseId,id,ct);

         return certifcate is not null ? Ok(certifcate) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(CertficateResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [EndpointSummary("Create Certfiecate a student in a course")]
    [EndpointDescription("Returns 404 if the course does not exist.")]
    public async Task<IActionResult> CreateCertficate(int courseId, CreateCertficateRequest request, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course not found",
                Detail = $"No course exits with id {courseId}",
                Status = StatusCodes.Status404NotFound
            }
            
            );
        }

        var certficate = await certficateService.CreateAsync(courseId, request,ct);
        if (certficate is null)
        {
           return NotFound(new ProblemDetails
            {
                Title = "Student not found",
                Detail = $"No student exists with id {request.StudentId}.",
                Status = StatusCodes.Status404NotFound
            });
        }
        return CreatedAtAction(nameof(GetCertficate),new {courseId, id = certficate.Id},certficate);
         throw new NotImplementedException();
    }
      [HttpGet(Name = "ListCourseCertficate")]
    [ProducesResponseType(typeof(List<CertficateResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List Certficate for a course")]
    public async Task<IActionResult> GetCertifcates(int courseId, CancellationToken ct)
    {
          var course = await courseService.GetByIdAsync(courseId, ct);
        if (course is null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Course not found",
                Detail = $"No course exists with id {courseId}.",
                Status = StatusCodes.Status404NotFound
            });
        }

        var certificate = await certficateService.GetByCourseAsync(courseId, ct);
        return Ok(certificate);
         throw new NotImplementedException();
    }

}
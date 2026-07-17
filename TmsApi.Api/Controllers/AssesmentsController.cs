using Microsoft.AspNetCore.Mvc;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers;

[ApiController]
[Route("api/courses/{courseId:int}/assessments")]
[Tags("Assessments")]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public class AssessmentsController(
    ICourseService courseService,
    IAssessmentService assessmentService) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetAssessment))]
    [ProducesResponseType(typeof(AssessmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Get one assessment for a course")]
    public async Task<IActionResult> GetAssessment(int courseId, int id, CancellationToken ct)
    {
        var assessment = await assessmentService.GetByIdAsync(courseId, id, ct);
        return assessment is not null ? Ok(assessment) : NotFound();
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssessmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Create an assessment for a course")]
    [EndpointDescription("Returns 404 if the course does not exist.")]
    public async Task<IActionResult> CreateAssessment(int courseId, CreateAssessmentRequest request, CancellationToken ct)
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

        var assessment = await assessmentService.CreateAsync(courseId, request, ct);
        return CreatedAtAction(nameof(GetAssessment), new { courseId, id = assessment.Id }, assessment);
    }

    [HttpGet(Name = "ListCourseAssessments")]
    [ProducesResponseType(typeof(List<AssessmentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("List assessments for a course")]
    public async Task<IActionResult> GetAssessments(int courseId, CancellationToken ct)
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

        var assessments = await assessmentService.GetByCourseAsync(courseId, ct);
        return Ok(assessments);
    }


}
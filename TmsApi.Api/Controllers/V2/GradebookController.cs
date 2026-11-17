using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Filters;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/gradebook")]
[Authorize]
[GradebookExceptionFilter]
public class GradebookController(IGradebookService service) : ControllerBase
{
    private GradebookActor Actor => new(
        User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Missing user id."),
        IsAdmin: User.IsInRole("Admin") || User.IsInRole("SuperAdmin"),
        IsStudent: User.IsInRole("Student"));

    private const string Staff = "Admin,Instructor,SuperAdmin";

    // ── Reads ──

    [HttpGet("course/{id:int}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> GetCourse(int id, CancellationToken ct)
        => Ok(await service.GetCourseGradebookAsync(id, Actor, ct));

    [HttpGet("student/{id:int}")]
    [Authorize(Roles = Staff + ",Student")]
    public async Task<IActionResult> GetStudent(int id, CancellationToken ct)
        => Ok(await service.GetStudentGradesAsync(id, Actor, ct));

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
        => Ok(await service.GetMyGradesAsync(Actor, ct));

    // ── Assessment-level grading ──

    [HttpPost]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Save(
        [FromBody] SaveGradeRequest request, CancellationToken ct)
        => Ok(await service.SaveGradeAsync(request, Actor, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Update(
        int id, [FromBody] UpdateGradeRequest request, CancellationToken ct)
        => Ok(await service.UpdateGradeAsync(id, request, Actor, ct));

    [HttpPost("{id:int}/publish")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> PublishAssessmentGrade(int id, CancellationToken ct)
    {
        await service.PublishAssessmentGradeAsync(id, Actor, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/correct")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Correct(
        int id, [FromBody] CorrectGradeRequest request, CancellationToken ct)
        => Ok(await service.CorrectGradeAsync(id, request, Actor, ct));

    // ── Final grade ──

    [HttpPost("enrollments/{enrollmentId:int}/publish")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> PublishFinal(int enrollmentId, CancellationToken ct)
    {
        var final = await service.PublishFinalGradeAsync(enrollmentId, Actor, ct);
        return Ok(new { enrollmentId, finalGrade = final });
    }

    // ── Appeals ──

    [HttpPost("enrollments/{enrollmentId:int}/appeals")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitAppeal(
        int enrollmentId, [FromBody] SubmitGradeAppealRequest request, CancellationToken ct)
    {
        var id = await service.SubmitAppealAsync(enrollmentId, request, Actor, ct);
        return Created($"/api/v2/gradebook/appeals/{id}", new { id });
    }

    [HttpGet("appeals/mine")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MyAppeals(CancellationToken ct)
        => Ok(await service.GetMyAppealsAsync(Actor, ct));

    [HttpGet("appeals")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> Appeals(
        [FromQuery] string? status, [FromQuery] int? courseId, CancellationToken ct)
        => Ok(await service.GetAppealsAsync(status, courseId, Actor, ct));

    [HttpPut("appeals/{id:int}")]
    [Authorize(Roles = Staff)]
    public async Task<IActionResult> ReviewAppeal(
        int id, [FromBody] ReviewGradeAppealRequest request, CancellationToken ct)
    {
        await service.ReviewAppealAsync(id, request, Actor, ct);
        return NoContent();
    }
}
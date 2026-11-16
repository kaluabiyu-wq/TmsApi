
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Api.Authorization;
using TmsApi.Application.DTOs;
using TmsApi.Application.Waitlists;

namespace TmsApi.Api.Controllers.V2;

/// <summary>
/// Student waitlist operations and staff queue management.
/// </summary>
[ApiController]
[Authorize]
[Route("api/v{version:apiVersion}/waitlists")]
[ApiVersion("2.0")]
public sealed class WaitlistsController(IWaitlistService waitlists) : ControllerBase
{
    // StudentId is currently supplied by the client because the existing Student
    // entity is not linked to TmsUser. Bind Student to the authenticated account
    // in the identity integration phase before removing this compatibility behavior.

    /// <summary>
    /// Adds a student to a course waitlist.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Join(
        [FromBody] JoinWaitlistRequest request,
        CancellationToken ct)
    {
        var result = await waitlists.JoinWaitlistAsync(request, ct);

        return result.Match<IActionResult>(
            ok => CreatedAtAction(
                nameof(GetForStudent),
                new
                {
                    studentId = ok.StudentId,
                    version = "2"
                },
                ok),
            error => ProblemFor(error));
    }

    /// <summary>
    /// Gets the paginated waitlist for a course.
    /// </summary>
    [HttpGet("course/{courseId:int}")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<ActionResult<PagedResponse<WaitlistResponse>>> GetForCourse(
        int courseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await waitlists.GetCourseWaitlistAsync(
            courseId,
            page,
            pageSize,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Gets all waitlist entries belonging to a student.
    /// </summary>
    [HttpGet("student/{studentId:int}")]
    public async Task<ActionResult<List<WaitlistResponse>>> GetForStudent(
        int studentId,
        CancellationToken ct)
    {
        var result = await waitlists.GetStudentWaitlistAsync(
            studentId,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Accepts an active seat offer.
    /// </summary>
    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(
        int id,
        CancellationToken ct)
    {
        var result = await waitlists.AcceptOfferAsync(id, ct);

        return result.Match<IActionResult>(
            value => Ok(value),
            error => ProblemFor(error));
    }

    /// <summary>
    /// Cancels a waitlist entry.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(
        int id,
        CancellationToken ct)
    {
        var result = await waitlists.CancelWaitlistAsync(id, ct);

        return result.Match<IActionResult>(
            value => Ok(value),
            error => ProblemFor(error));
    }

    /// <summary>
    /// Offers a seat to a specific waitlist entry.
    /// </summary>
    [HttpPost("{id:int}/offer")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<IActionResult> Offer(
        int id,
        CancellationToken ct)
    {
        var result = await waitlists.OfferSeatAsync(id, ct);

        return result.Match<IActionResult>(
            value => Ok(value),
            error => ProblemFor(error));
    }

    /// <summary>
    /// Offers the next available seat to eligible waitlisted students.
    /// </summary>
    [HttpPost("course/{courseId:int}/offer-next")]
    [Authorize(Policy = Policies.InstructorOrAdmin)]
    public async Task<IActionResult> OfferNext(
        int courseId,
        CancellationToken ct)
    {
        var result = await waitlists.OfferNextSeatAsync(
            courseId,
            ct);

        return result.Match<IActionResult>(
            value => Ok(value),
            error => ProblemFor(error));
    }

    /// <summary>
    /// Converts a waitlist error into an appropriate HTTP response.
    /// </summary>
    private ObjectResult ProblemFor(WaitlistError error)
    {
        var status = error.Code switch
        {
            "course_not_found" or
            "student_not_found" or
            "entry_not_found" =>
                StatusCodes.Status404NotFound,

            "already_enrolled" or
            "already_waitlisted" or
            "course_has_seats" or
            "no_seat_available" or
            "invalid_status" or
            "offer_expired" =>
                StatusCodes.Status409Conflict,

            _ => StatusCodes.Status400BadRequest
        };

        return Problem(
            statusCode: status,
            title: "Waitlist request rejected",
            detail: error.Message,
            type: $"https://tms.local/errors/{error.Code}");
    }
}

// using Asp.Versioning;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using TmsApi.Api.Authorization;
// using TmsApi.Application.Common;
// using TmsApi.Application.DTOs;
// using TmsApi.Application.Waitlists;
// using TmsApi.Application.Waitlists.DTOs;
// using TmsApi.Application.Waitlists.Interfaces;

// namespace TmsApi.Api.Controllers.V2;

// [ApiController]
// [Route("api/v{version:apiVersion}/waitlists")]
// [ApiVersion("2.0")]
// public class WaitlistsController(IWaitlistService waitlists) : ControllerBase
// {
//     // NOTE: the API currently has no link between a login (TmsUser) and a Student row, so the
//     // student endpoints trust the ids in the request - exactly like POST /enrollments does today.
//     // Add Student.UserId + an ownership check in Phase 12.

//     /// <summary>Join the queue of a full course.</summary>
//     [HttpPost]
//     public async Task<ActionResult<WaitlistResponse>> Join([FromBody] JoinWaitlistRequest request, CancellationToken ct)
//     {
//         var result = await waitlists.JoinWaitlistAsync(request, ct);
//         return result.Match<ActionResult<WaitlistResponse>>(
//             ok => CreatedAtAction(nameof(GetForStudent), new { studentId = ok.StudentId }, ok),
//             ProblemFor);
//     }

//     [HttpGet("course/{courseId:int}")]
//     [Authorize(Policy = Policies.InstructorOrAdmin)]
//     public async Task<ActionResult<PagedResponse<WaitlistResponse>>> GetForCourse(
//         int courseId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
//         Ok(await waitlists.GetCourseWaitlistAsync(courseId, page, pageSize, ct));

//     [HttpGet("student/{studentId:int}")]
//     public async Task<ActionResult<List<WaitlistResponse>>> GetForStudent(int studentId, CancellationToken ct) =>
//         Ok(await waitlists.GetStudentWaitlistAsync(studentId, ct));

//     [HttpPost("{id:int}/accept")]
//     public async Task<ActionResult<WaitlistResponse>> Accept(int id, CancellationToken ct) =>
//         (await waitlists.AcceptOfferAsync(id, ct)).Match<ActionResult<WaitlistResponse>>(Ok, ProblemFor);

//     [HttpDelete("{id:int}")]
//     public async Task<ActionResult<WaitlistResponse>> Cancel(int id, CancellationToken ct) =>
//         (await waitlists.CancelWaitlistAsync(id, ct)).Match<ActionResult<WaitlistResponse>>(Ok, ProblemFor);

//     /// <summary>Staff: offer a free seat to this specific entry.</summary>
//     [HttpPost("{id:int}/offer")]
//     [Authorize(Policy = Policies.InstructorOrAdmin)]
//     public async Task<ActionResult<WaitlistResponse>> Offer(int id, CancellationToken ct) =>
//         (await waitlists.OfferSeatAsync(id, ct)).Match<ActionResult<WaitlistResponse>>(Ok, ProblemFor);

//     /// <summary>Staff: fill every free seat of the course from the front of the queue.</summary>
//     [HttpPost("course/{courseId:int}/offer-next")]
//     [Authorize(Policy = Policies.InstructorOrAdmin)]
//     public async Task<ActionResult<IReadOnlyList<WaitlistResponse>>> OfferNext(int courseId, CancellationToken ct) =>
//         (await waitlists.OfferNextSeatAsync(courseId, ct)).Match<ActionResult<IReadOnlyList<WaitlistResponse>>>(Ok, ProblemFor);

//     private ObjectResult ProblemFor(WaitlistError error)
//     {
//         var status = error.Code switch
//         {
//             "course_not_found" or "student_not_found" or "entry_not_found" => StatusCodes.Status404NotFound,
//             "already_enrolled" or "already_waitlisted" or "course_has_seats" or "no_seat_available"
//                 or "invalid_status" or "offer_expired" => StatusCodes.Status409Conflict,
//             _ => StatusCodes.Status400BadRequest
//         };
//         return Problem(statusCode: status, title: "Waitlist request rejected", detail: error.Message,
//             type: $"https://tms.local/errors/{error.Code}");
//     }
// }
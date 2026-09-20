using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Courses.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CourseController(ICachedCourseService cachedCourseService, TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var all = await cachedCourseService.GetAllCoursesAsync(ct);
        var totalCount = all.Count;

        var rows = all
            .OrderBy(c => c.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

     var paged = new PagedResponse<CourseResponseDto>
     {
       Items = rows,
       TotalCount = totalCount,
       Page = page,
       PageSize = pageSize  
     };
        return Ok(new
        {
            items = paged.Items,
            totalCount = paged.TotalCount,
            page = paged.Page,
            pageSize = paged.PageSize,
            totalPages = paged.TotalPages,
            hasNext = paged.HasNext,
            hasPrevious = paged.HasPrevious,
             links = new
            {
                self = $"/api/v2/courses?page={page}&pageSize={pageSize}",
                next = paged.HasNext ? $"/api/v2/courses?page={page + 1}&pageSize={pageSize}" : (string?)null,
                prev = paged.HasPrevious ? $"/api/v2/courses?page={page - 1}&pageSize={pageSize}" : (string?)null,
                enroll = "/api/v2/enrollments"
            }
        });
    }
    [Authorize(Roles = "Instructor,Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateCourse(
        [FromBody] CreateCourseRequest request,
        [FromServices] ICourseService courseService,
        [FromServices] ICachedCourseService cachedCourseService,
        CancellationToken ct)
    {
      
        var instructorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(instructorId))
        {
            return Unauthorized();
        }

        if (await courseService.CodeExistAsync(request.Code, ct))
        {
            ModelState.AddModelError(nameof(request.Code), $"Course code '{request.Code}' already exists.");
            return ValidationProblem(ModelState);
        }

        var created = await courseService.CreateAsync(request, instructorId, ct);

        await cachedCourseService.InvalidateCourseCachedAsync(ct);

        return CreatedAtAction(nameof(GetCourses), new { }, created);
    }

 [Authorize(Roles = "Instructor,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        [FromBody] UpdateCourseRequestBody body,
        [FromServices] ICourseService courseService,
        [FromServices] ICachedCourseService cachedCourseService,
        [FromServices] IAuthorizationService authorizationService,
        CancellationToken ct)
    {
        var course = await context.Courses.FindAsync(new object?[] { id }, ct);
        if (course == null) return NotFound();

        var authResult = await authorizationService.AuthorizeAsync(User, course, "CanEditCourse");
        if (!authResult.Succeeded)
        {
            return Forbid(); 
        }

        var updated = await courseService.UpdateAsync(new UpdateCourseCommand(id, body.Title), ct);

        await cachedCourseService.InvalidateCourseCachedAsync(ct);

        return Ok(updated);
    }

    public record UpdateCourseRequestBody(string Title);
}
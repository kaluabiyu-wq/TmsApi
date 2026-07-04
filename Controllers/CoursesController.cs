using Microsoft.AspNetCore.Mvc;
using TmsApi.Entities;

namespace Tms.Api.Controllers;

[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseServices courseService) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetCourseById))]
    public async Task<IActionResult> GetCourseById(int id, CancellationToken ct)
    {
        var course = await courseService.GetByIdAsync(id, ct);

        return course is not null
            ? Ok(course)
            : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(Course course, CancellationToken ct)
    {
        var result = await courseService.CreateAsync(course, ct);

        return CreatedAtAction(
            nameof(GetCourseById),
            new { id = result.Id },
            result);
    }
}
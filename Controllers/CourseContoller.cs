
using Microsoft.AspNetCore.Mvc;

namespace TmsApi.Controllers;


[ApiController]
[Route("api/courses")]
public class CoursesController(ICourseService courseService) : ControllerBase
{
  
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var courses = await courseService.GetAllAsync();
        return Ok(courses);
    }

  
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var course = await courseService.GetByIdAsync(id);
        return course is not null ? Ok(course) : NotFound();
    }

 
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCourseRequest request)
    {
        var course = await courseService.CreateAsync(
            request.Title,
            request.Capacity);

        return CreatedAtAction(
            nameof(GetById),
            new { id = course.Id },
            course);
    }

   
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await courseService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }

    public record CreateCourseRequest(string Title,int Capacity);
}
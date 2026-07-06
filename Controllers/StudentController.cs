
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;


namespace TmsApi.Controllers;


[ApiController]
[Route("api/students")]
public class StudentsController(IStudentService studentService,TmSDbContext context) : ControllerBase
{
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var students = await studentService.GetAllAsync();
        return Ok(students);
    }

   
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var student = await studentService.GetByIdAsync(id);
        return student is not null ? Ok(student) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStudentRequest request)
    {
        var student = await studentService.CreateAsync(
            request.Name,
            request.Gpa);

        return CreatedAtAction(
            nameof(GetById),
            new { id = student.Id },
            student);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await studentService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
    
 [HttpPut("{id}")]
public async Task<IActionResult> Update(string id,  CreateStudentRequest request)
{
    var student = await context.Students.FindAsync(id);
    if (student is null) return NotFound();

    student.Name = request.Name;
    student.GPA = request.Gpa;
    try
    {
        await context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException)
    {
        return Conflict("The record was modified by another user.");
    }

    return Ok(student);
}
    public record CreateStudentRequest(string Name, decimal Gpa);


}
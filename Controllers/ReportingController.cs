
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;



namespace TmsApi.Controllers;

[ApiController]
[Route("api/report")]

public class ReportingController(TmSDbContext context): ControllerBase
{
    [HttpGet("active-student")]
    public async Task<IActionResult> Active()
    {
      Console.WriteLine("\n>>> Active students are...");
      var count = await context.Students
      .Where(s=>s.IsActive && s.GPA >= 3.0m)
      .CountAsync();

      return Ok(count);
    }
    [HttpGet("course-enrollment-descending-order")]
    public async Task<IActionResult> CourseEnrollmentOrder()
    {
         Console.WriteLine("\n>>> Course enrollments in descending order...");
     var list = await context.Courses.Select(c => new
     {
         c.Title,EnrollmentCount = c.Enrollments.Count
     }).OrderByDescending(x => x.EnrollmentCount).ToListAsync();

     return Ok(list);

    }
    [HttpGet("average-gpa")]
    public async Task<IActionResult> Average()
    {
         Console.WriteLine("\n>>> Average Gpa...");
        var list  = await context.Enrollments.GroupBy(e => e.Course.Title)
        .Select(g => new
        {
            Course = g.Key,
            AverageGPA = g.Average(e => e.Student.GPA)
        }).ToListAsync();

        return Ok(list);
    }
    [HttpGet("zero-enrollment-usingSubquery")]
    public async Task<IActionResult> UsingSubquery()
    {
           Console.WriteLine("\n>>> students that have zero enrollments using Subquery.");
        var list = await context.Students.Where(s =>!s.Enrollments.Any())
                   .Select(s => s.Name).ToListAsync();
        return Ok(list);
    }
    [HttpGet("zero-enrollment-usingEfCoreLeftJoin")]
    public async Task<IActionResult> UsingEfCoreLeftJoin()
    {
         Console.WriteLine("\n>>> students that have zero enrollments using Using EF Core 10 LeftJoin.");
    
        var list = await context.Students.LeftJoin(context.Enrollments,
        s => s.ID, e => e.StudentId, 
        (s,e) => new { s, e}).Where(x => x.e == null)
        .Select(x => x.s.Name).ToListAsync();

        return Ok(list);
    }

}
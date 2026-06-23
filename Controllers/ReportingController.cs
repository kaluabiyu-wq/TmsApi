
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
 [HttpGet("Paginate")]
    public async Task<IActionResult> GetTopCourses(
    CancellationToken ct = default)
    {
 var courseStats = await context.Enrollments
 .GroupBy(e => e.CourseId)
 .Select(g => new
 {
     CourseId = g.Key,
     StudentCount = g.Count(),
     AverageGpa = g.Average(e => e.Student.GPA)

 }).OrderByDescending(s => s.StudentCount)
 .Take(5)
  .ToListAsync(ct);

  return Ok(courseStats);

    }

     [HttpGet("Paged")]

  public async Task<IActionResult> Group(
   [FromQuery]  int pageSize = 25,
    [FromQuery] int pageNumber = 2,
    CancellationToken ct = default) {
     
  var page = await context.Students
  .OrderBy(s => s.Name)
  .Skip((pageNumber - 1) * pageSize)
  .Take(pageSize)
  .ToListAsync(ct);

  return Ok(page);

}

[HttpGet("round-trip")]
public async Task<IActionResult> RoundTrip(CancellationToken ct = default)
    {

        var students = await context.Students.AsNoTracking().ToListAsync(ct);
        foreach (var s in students)
        {
            
            var count = await context.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.StudentId == s.ID,ct);
            Console.WriteLine($"{s.Name}: {count}  enrollments");
        }

        return Ok(students);
        
    }

[HttpGet("shaped-query")]
public async Task<IActionResult> Shapedquery(CancellationToken  ct = default)
    {

        var report = await context.Students.AsNoTracking().Select(
            s=> new
            {
                s.Name,EnrollmentCount = s.Enrollments.Count
            }
        ).ToListAsync(ct);
        foreach (var r in report)
        {
        
            Console.WriteLine($"{r.Name}: {r.EnrollmentCount}  enrollments");
        }

        return Ok(report);
        
    }


[HttpGet("using-include")]
public async Task<IActionResult> UsingInclude(CancellationToken  ct = default)
    {

        var students = await context.Students.AsNoTracking().Include(
            s=> s.Enrollments ).ToListAsync(ct);
        foreach (var s in students)
        {
        
            Console.WriteLine($"{s.Name}: {s.Enrollments.Count}  enrollments");
        }

        return Ok(students);
        
    }


}
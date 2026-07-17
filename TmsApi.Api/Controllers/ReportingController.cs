
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Domain.Entities;

namespace TmsApi.Api.Controllers;



[ApiController]
[Route("api/report")]

public class ReportingController(TmSDbContext context) : ControllerBase
{
    [HttpGet("active-student")]
    public async Task<IActionResult> Active()
    {
        Console.WriteLine("\n>>> Active students are...");
        var count = await context.Students
        .Where(s => s.IsActive && s.GPA >= 3.0m)
        .CountAsync();

        return Ok(count);
    }
    [HttpGet("course-enrollment-descending-order")]
    public async Task<IActionResult> CourseEnrollmentOrder()
    {
        Console.WriteLine("\n>>> Course enrollments in descending order...");
        var list = await context.Courses.Select(c => new
        {
            c.Title,
            EnrollmentCount = c.Enrollments.Count
        }).OrderByDescending(x => x.EnrollmentCount).ToListAsync();

        return Ok(list);

    }
    [HttpGet("average-gpa")]
    public async Task<IActionResult> Average()
    {
        Console.WriteLine("\n>>> Average Gpa...");
        var list = await context.Enrollments.GroupBy(e => e.Course.Title)
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
        var list = await context.Students.Where(s => !s.Enrollments.Any())
                   .Select(s => s.Name).ToListAsync();
        return Ok(list);
    }
    [HttpGet("zero-enrollment-usingEfCoreLeftJoin")]
    public async Task<IActionResult> UsingEfCoreLeftJoin()
    {
        Console.WriteLine("\n>>> students that have zero enrollments using Using EF Core 10 LeftJoin.");

        var list = await context.Students.LeftJoin(context.Enrollments,
        s => s.ID, e => e.StudentId,
        (s, e) => new { s, e }).Where(x => x.e == null)
        .Select(x => x.s.Name).ToListAsync();

        return Ok(list);
    }
    [HttpGet("Paginate")]
    public async Task<IActionResult> GetTopCourses(
       CancellationToken cancellationToken)
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
         .ToListAsync(cancellationToken);

        return Ok(courseStats);

    }

    [HttpGet("Paged")]
    public async Task<IActionResult> Group( CancellationToken cancellationToken,
    int pageSize = 25, int pageNumber = 1)
    {

        var page = await context.Students
        .OrderBy(s => s.Name)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

        return Ok(page);

    }

    [HttpGet("round-trip")]
    public async Task<IActionResult> RoundTrip(CancellationToken cancellationToken)
    {

        var students = await context.Students.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var s in students)
        {

            var count = await context.Enrollments
            .AsNoTracking()
            .CountAsync(e => e.StudentId == s.ID, cancellationToken);
            Console.WriteLine($"{s.Name}: {count}  enrollments");
        }

        return Ok(students);

    }

    [HttpGet("shaped-query")]
    public async Task<IActionResult> Shapedquery(CancellationToken cancellationToken)
    {

        var report = await context.Students.AsNoTracking().Select(
            s => new
            {
                s.Name,
                EnrollmentCount = s.Enrollments.Count
            }
        ).ToListAsync(cancellationToken);
        foreach (var r in report)
        {

            Console.WriteLine($"{r.Name}: {r.EnrollmentCount}  enrollments");
        }

        return Ok(report);

    }


    [HttpGet("using-include")]
    public async Task<IActionResult> UsingInclude(CancellationToken cancellationToken)
    {

       var students = await context.Students
    .Include(s => s.Enrollments)
    .ToListAsync(cancellationToken);
    
    foreach (var s in students)
    Console.WriteLine($"{s.Name}: {s.Enrollments.Count} enrollments");

return Ok(students);
    }
}
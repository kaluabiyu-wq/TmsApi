using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V1;

public record CreateStudentRequest(string RegistrationNumber, string Name);

[ApiController]
[Route("api/v{version:apiVersion}/students")]
[ApiVersion("1.0")]
public class StudentsController(TmsDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var students = await context.Students
            .AsNoTracking()
            .Select(s => new { s.ID, s.RegistrationNumber, s.Name })
            .ToListAsync(ct);

        return Ok(students);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateStudentRequest request,
        CancellationToken ct)
    {
        var alreadyExists = await context.Students
            .AnyAsync(s => s.RegistrationNumber == request.RegistrationNumber, ct);

        if (alreadyExists)
        {
            return Conflict(new
            {
                message = $"A student with registration number '{request.RegistrationNumber}' already exists.",
            });
        }

        var student = new Student
        {
            RegistrationNumber = request.RegistrationNumber,
            Name = request.Name,
            GPA = 0,
            IsActive = true,
        };

        context.Students.Add(student);
        await context.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetAll),
            new { version = "1.0" },
            new { student.ID, student.RegistrationNumber, student.Name });
    }
}
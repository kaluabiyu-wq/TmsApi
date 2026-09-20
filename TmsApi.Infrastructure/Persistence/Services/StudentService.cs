


using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TmsApi.Infrastructure.Persistence.Services;

public class StudentService(TmsDbContext context, ILogger<StudentService> logger) : IStudentService
{
    public Task<StudentResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Students
            .AsNoTracking()
            .Where(s => s.ID == id )
            .Select(s => new StudentResponseDto(s.ID,s.RegistrationNumber,s.Name,s.GPA,s.IsActive))
            .FirstOrDefaultAsync(ct);

  public async Task<StudentResponseDto> CreateAsync(CreateStudentRequest request, CancellationToken ct)
{
    var student = new Student
        {
           RegistrationNumber = request.RegistrationNumber,
           Name = request.Name,
           GPA = request.GPA,
          IsActive = request.IsActive
        };

        context.Students.Add(student);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created StudentId {StudentId} (Registration Number {RegistrationNumber}) Name {Name} Gpa {Gpa} , {IsActive}",
           student.ID,student.RegistrationNumber,student.Name,student.GPA,student.IsActive);

        return (await GetByIdAsync(student.ID, ct))!;
}

  public async  Task<List<StudentResponseDto>> GetStudent (int id,CancellationToken ct)

{
    return await context.Students
            .AsNoTracking()
            .Where(s => s.ID == id )
            .Select(s => new StudentResponseDto(s.ID,s.RegistrationNumber,s.Name,s.GPA,s.IsActive))
            .ToListAsync(ct);
        
}

}
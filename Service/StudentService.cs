using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

namespace TmsApi.Services;

public class StudentService(TmSDbContext context, ILogger<StudentService> logger) : IStudentService
{
    public async Task<StudentRecord> CreateAsync(string name, decimal gpa, CancellationToken ct)
    {
        var existing = await context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Name == name, ct);

        if (existing is not null)
        {
            logger.LogWarning(
                "Duplicate student {StudentName} already exists (record {StudentId})",
                name, existing.ID);

            return new StudentRecord(existing.ID, existing.Name, DateTime.UtcNow, existing.GPA);
        }

        var student = new Student
        {
            RegistrationNumber = Guid.NewGuid().ToString("N")[..12],
            Name = name,
            GPA = gpa
        };

        context.Students.Add(student);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created student {StudentName} with id {StudentId}",
            name, student.ID);

        return new StudentRecord(student.ID, student.Name, DateTime.UtcNow, student.GPA);
    }

    public Task<StudentRecord?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Students
            .AsNoTracking()
            .Where(s => s.ID == id)
            .Select(s => new StudentRecord(s.ID, s.Name, DateTime.UtcNow, s.GPA))
            .FirstOrDefaultAsync(ct);

    public Task<List<StudentRecord>> GetAllAsync(CancellationToken ct) =>
        context.Students
            .AsNoTracking()
            .Select(s => new StudentRecord(s.ID, s.Name, DateTime.UtcNow, s.GPA))
            .ToListAsync(ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var student = await context.Students.FindAsync([id], ct);
        if (student is null)
        {
            logger.LogWarning("Delete failed. Student {StudentId} not found", id);
            return false;
        }

        context.Students.Remove(student);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Deleted student {StudentId}", id);

        return true;
    }
}

public record StudentRecord(int Id, string Name, DateTime EnrollmentDate, decimal Gpa);
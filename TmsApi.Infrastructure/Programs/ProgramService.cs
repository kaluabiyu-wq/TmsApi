using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Programs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Programs;

public sealed class ProgramService(TmsDbContext db, IProgressService progress) : IProgramService
{
    public async Task<TrainingProgramDto> CreateProgramAsync(CreateProgramRequest request, CancellationToken ct = default)
    {
        var code = request.Code?.Trim().ToUpperInvariant();
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 40) throw new ArgumentException("A program code (maximum 40 characters) is required.");
        if (string.IsNullOrWhiteSpace(name) || name.Length > 180) throw new ArgumentException("A program name (maximum 180 characters) is required.");
        if (request.DurationMonths is <= 0 or > 120) throw new ArgumentException("Duration must be between 1 and 120 months.");
        if (await db.TrainingPrograms.AnyAsync(x => x.Code == code, ct)) throw new InvalidOperationException("A training program with this code already exists.");

        var entity = new TrainingProgram { Code = code, Name = name, Description = request.Description?.Trim() ?? string.Empty, DurationMonths = request.DurationMonths };
        db.TrainingPrograms.Add(entity);
        await db.SaveChangesAsync(ct);
        return await GetProgramDtoAsync(entity.Id, ct) ?? throw new InvalidOperationException("The program could not be loaded after creation.");
    }

    public async Task<IReadOnlyList<TrainingProgramDto>> GetProgramsAsync(bool includeInactive = false, CancellationToken ct = default)
    {
        var ids = await db.TrainingPrograms.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Name).Select(x => x.Id).ToListAsync(ct);
        var result = new List<TrainingProgramDto>(ids.Count);
        foreach (var id in ids) { var item = await GetProgramDtoAsync(id, ct); if (item is not null) result.Add(item); }
        return result;
    }

    public async Task<TrainingProgramDto?> AddCourseToProgramAsync(int programId, AddProgramCourseRequest request, CancellationToken ct = default)
    {
        if (request.Sequence < 1) throw new ArgumentException("Course sequence must be at least 1.");
        var program = await db.TrainingPrograms.FirstOrDefaultAsync(x => x.Id == programId, ct);
        if (program is null) return null;
        if (!await db.Courses.AnyAsync(x => x.Id == request.CourseId, ct)) throw new ArgumentException("The selected course does not exist.");
        if (await db.ProgramCourses.AnyAsync(x => x.ProgramId == programId && x.CourseId == request.CourseId, ct)) throw new InvalidOperationException("This course is already part of the program.");
        if (await db.ProgramCourses.AnyAsync(x => x.ProgramId == programId && x.Sequence == request.Sequence, ct)) throw new InvalidOperationException("That sequence number is already used in this program.");
        db.ProgramCourses.Add(new ProgramCourse { ProgramId = programId, CourseId = request.CourseId, Sequence = request.Sequence, IsRequired = request.IsRequired });
        program.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetProgramDtoAsync(programId, ct);
    }

    public async Task<StudentProgramProgressDto> RegisterStudentAsync(int programId, int studentId, CancellationToken ct = default)
    {
        if (!await db.TrainingPrograms.AnyAsync(x => x.Id == programId && x.IsActive, ct)) throw new ArgumentException("The training program does not exist or is inactive.");
        if (!await db.Students.AnyAsync(x => x.ID == studentId && !x.IsDeleted, ct)) throw new ArgumentException("The student does not exist.");
        if (!await db.ProgramCourses.AnyAsync(x => x.ProgramId == programId && x.IsRequired, ct)) throw new InvalidOperationException("Add at least one required course before registering students.");
        var existing = await db.StudentProgramEnrollments.Include(x => x.ProgressItems)
            .FirstOrDefaultAsync(x => x.ProgramId == programId && x.StudentId == studentId, ct);
        if (existing is not null && existing.Status != "Withdrawn")
            throw new InvalidOperationException("The student already has an active or completed enrollment in this program.");
        StudentProgramEnrollment enrollment;
        if (existing is not null)
        {
            enrollment = existing;
            enrollment.Status = "Active";
            enrollment.CompletedAt = null;
            enrollment.WithdrawnAt = null;
            db.StudentProgress.RemoveRange(enrollment.ProgressItems);
            enrollment.ProgressItems.Clear();
        }
        else
        {
            enrollment = new StudentProgramEnrollment { ProgramId = programId, StudentId = studentId };
            db.StudentProgramEnrollments.Add(enrollment);
        }
        await db.SaveChangesAsync(ct);
        return await progress.RecalculateProgressAsync(enrollment.Id, ct) ?? throw new InvalidOperationException("Unable to calculate initial student progress.");
    }

    private async Task<TrainingProgramDto?> GetProgramDtoAsync(int id, CancellationToken ct)
    {
        var program = await db.TrainingPrograms.AsNoTracking().Include(x => x.ProgramCourses).ThenInclude(x => x.Course)
            .Include(x => x.StudentEnrollments).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (program is null) return null;
        var courses = program.ProgramCourses.OrderBy(x => x.Sequence).Select(x => new ProgramCourseProgressDto(x.Id, x.CourseId, x.Course.Code, x.Course.Title, x.Sequence, x.IsRequired, "NotStarted", null, null)).ToList();
        return new(program.Id, program.Code, program.Name, program.Description, program.DurationMonths, program.IsActive, courses.Count, program.StudentEnrollments.Count(x => x.Status != "Withdrawn"), courses);
    }
}

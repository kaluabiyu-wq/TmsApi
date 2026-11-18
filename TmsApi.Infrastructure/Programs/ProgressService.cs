using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Programs;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Programs;

public sealed class ProgressService(TmsDbContext db) : IProgressService
{
    public async Task<IReadOnlyList<StudentProgramProgressDto>> GetStudentProgressAsync(int studentId, CancellationToken ct = default)
    {
        var ids = await db.StudentProgramEnrollments.AsNoTracking().Where(x => x.StudentId == studentId).OrderByDescending(x => x.EnrolledAt).Select(x => x.Id).ToListAsync(ct);
        var result = new List<StudentProgramProgressDto>(ids.Count);
        foreach (var id in ids) { var item = await RecalculateProgressAsync(id, ct); if (item is not null) result.Add(item); }
        return result;
    }

    public async Task<IReadOnlyList<StudentProgramProgressDto>> GetProgramProgressAsync(int programId, CancellationToken ct = default)
    {
        var ids = await db.StudentProgramEnrollments.AsNoTracking().Where(x => x.ProgramId == programId).OrderBy(x => x.Student.Name).Select(x => x.Id).ToListAsync(ct);
        var result = new List<StudentProgramProgressDto>(ids.Count);
        foreach (var id in ids) { var item = await RecalculateProgressAsync(id, ct); if (item is not null) result.Add(item); }
        return result;
    }

    public Task<StudentProgramProgressDto?> CheckProgramCompletionAsync(int studentProgramEnrollmentId, CancellationToken ct = default)
        => RecalculateProgressAsync(studentProgramEnrollmentId, ct);

    public async Task<StudentProgramProgressDto?> RecalculateProgressAsync(int studentProgramEnrollmentId, CancellationToken ct = default)
    {
        var enrollment = await db.StudentProgramEnrollments
            .Include(x => x.Student).Include(x => x.Program).ThenInclude(x => x.ProgramCourses).ThenInclude(x => x.Course)
            .Include(x => x.ProgressItems)
            .FirstOrDefaultAsync(x => x.Id == studentProgramEnrollmentId, ct);
        if (enrollment is null) return null;

        var programCourses = enrollment.Program.ProgramCourses.OrderBy(x => x.Sequence).ToList();
        var courseIds = programCourses.Select(x => x.CourseId).ToArray();
        var courseEnrollments = await db.Enrollments.AsNoTracking()
            .Where(x => x.StudentId == enrollment.StudentId && courseIds.Contains(x.CourseId) && !x.IsArchived)
            .ToDictionaryAsync(x => x.CourseId, ct);
        var now = DateTime.UtcNow;

        foreach (var programCourse in programCourses)
        {
            var item = enrollment.ProgressItems.FirstOrDefault(x => x.ProgramCourseId == programCourse.Id);
            if (item is null)
            {
                item = new TmsApi.Domain.Entities.StudentProgress { StudentProgramEnrollmentId = enrollment.Id, ProgramCourseId = programCourse.Id };
                enrollment.ProgressItems.Add(item);
            }
            courseEnrollments.TryGetValue(programCourse.CourseId, out var courseEnrollment);
            var status = courseEnrollment is not null && string.Equals(courseEnrollment.Status, "Completed", StringComparison.OrdinalIgnoreCase)
                ? "Completed"
                : courseEnrollment is not null && (string.Equals(courseEnrollment.Status, "Approved", StringComparison.OrdinalIgnoreCase) || string.Equals(courseEnrollment.Status, "InProgress", StringComparison.OrdinalIgnoreCase))
                    ? "InProgress" : "NotStarted";
            item.Status = status;
            item.StartedAt = status == "NotStarted" ? null : item.StartedAt ?? courseEnrollment?.EnrolledAt ?? now;
            item.CompletedAt = status == "Completed" ? item.CompletedAt ?? now : null;
            item.LastCalculatedAt = now;
        }

        var required = programCourses.Where(x => x.IsRequired).ToList();
        var completedIds = enrollment.ProgressItems.Where(x => x.Status == "Completed").Select(x => x.ProgramCourseId).ToHashSet();
        var completedRequired = required.Count(x => completedIds.Contains(x.Id));
        var allRequiredCompleted = required.Count > 0 && completedRequired == required.Count;
        if (enrollment.Status != "Withdrawn")
        {
            enrollment.Status = allRequiredCompleted ? "Completed" : "Active";
            enrollment.CompletedAt = allRequiredCompleted ? enrollment.CompletedAt ?? now : null;
        }
        await db.SaveChangesAsync(ct);
        return Map(enrollment, programCourses);
    }

    private static StudentProgramProgressDto Map(TmsApi.Domain.Entities.StudentProgramEnrollment enrollment, IReadOnlyList<TmsApi.Domain.Entities.ProgramCourse> programCourses)
    {
        var progressByCourse = enrollment.ProgressItems.ToDictionary(x => x.ProgramCourseId);
        var courses = programCourses.Select(pc =>
        {
            progressByCourse.TryGetValue(pc.Id, out var item);
            return new ProgramCourseProgressDto(pc.Id, pc.CourseId, pc.Course.Code, pc.Course.Title, pc.Sequence, pc.IsRequired, item?.Status ?? "NotStarted", item?.StartedAt, item?.CompletedAt);
        }).ToList();
        var required = courses.Where(x => x.IsRequired).ToList();
        var completed = required.Count(x => x.Status == "Completed");
        var percent = required.Count == 0 ? 0m : Math.Round(completed * 100m / required.Count, 2);
        var current = courses.FirstOrDefault(x => x.IsRequired && x.Status == "InProgress") ?? courses.FirstOrDefault(x => x.IsRequired && x.Status == "NotStarted");
        var eligible = required.Count > 0 && completed == required.Count;
        return new(enrollment.Id, enrollment.StudentId, enrollment.Student.Name, enrollment.ProgramId, enrollment.Program.Code, enrollment.Program.Name, enrollment.Status, required.Count, completed, required.Count - completed, percent, eligible, current?.CourseTitle, courses);
    }
}

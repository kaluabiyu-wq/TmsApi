using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;


namespace TmsApi.Infrastructure.Persistence.Services;

public class CourseAuthorization(TmsDbContext db) : ICourseAuthorization
{
    public async Task EnsureCanManageCourseAsync(
        GradebookActor actor, int courseId, CancellationToken ct)
    {
        if (actor.IsAdmin) return;

        var allowed = await db.Courses.AnyAsync(
            c => c.Id == courseId && c.InstructorId == actor.UserId, ct);

        if (!allowed)
            throw new UnauthorizedAccessException(
                "You are not assigned to this course.");
    }

    public async Task<IReadOnlyList<int>?> GetManagedCourseIdsAsync(
        GradebookActor actor, CancellationToken ct)
    {
        if (actor.IsAdmin) return null;

        return await db.Courses
            .Where(c => c.InstructorId == actor.UserId)
            .Select(c => c.Id)
            .ToListAsync(ct);
    }
}
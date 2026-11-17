using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface ICourseAuthorization
{
    Task EnsureCanManageCourseAsync(
        GradebookActor actor, int courseId, CancellationToken ct);

    // null => all courses (Admin).
    Task<IReadOnlyList<int>?> GetManagedCourseIdsAsync(
        GradebookActor actor, CancellationToken ct);
}
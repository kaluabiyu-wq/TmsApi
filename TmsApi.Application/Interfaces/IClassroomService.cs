using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface IClassroomService
{
    Task<PagedResponse<ClassroomResponse>> GetPageAsync(
        int page, int pageSize, bool? isActive, CancellationToken ct);

    Task<ClassroomResponse?> GetByIdAsync(int id, CancellationToken ct);

    /// <exception cref="InvalidOperationException">A classroom with the same name already exists (mapped to 409).</exception>
    Task<ClassroomResponse> CreateAsync(CreateClassroomRequest request, CancellationToken ct);

    /// <returns>The updated classroom, or null if it does not exist.</returns>
    /// <exception cref="InvalidOperationException">Another classroom already uses the name (mapped to 409).</exception>
    Task<ClassroomResponse?> UpdateAsync(int id, UpdateClassroomRequest request, CancellationToken ct);
}

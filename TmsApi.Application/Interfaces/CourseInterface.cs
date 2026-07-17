

using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;
public interface ICourseService
{
Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);
Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);


Task<bool> CodeExistAsync (string Code,CancellationToken ct);
Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct);
}
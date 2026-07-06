

using Tms.Api.Dtos;
using TmsApi.Entities;

public interface ICourseService
{
Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);
Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);


Task<bool> CodeExistAsync (string Code,CancellationToken ct);
}
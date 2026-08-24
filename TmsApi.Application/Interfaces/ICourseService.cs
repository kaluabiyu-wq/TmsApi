

using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Application.Courses.Commands;


namespace TmsApi.Application.Interfaces;
public interface ICourseService
{
Task<CourseResponseDto?> GetByCodeAsync(string code, CancellationToken ct);
Task<CourseResponseDto> CreateAsync(CreateCourseRequest request,string instructorId, CancellationToken ct);


Task<bool> CodeExistAsync (string Code,CancellationToken ct);
Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct);
Task<List<CourseResponseDto>> GetAllAsync(CancellationToken ct);
Task<CourseResponseDto> UpdateAsync(UpdateCourseCommand command, CancellationToken ct);
Task DeleteAsync(int id, CancellationToken ct);
}
using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface IAssessmentService
{
    Task<AssessmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct);

    Task<AssessmentResponseDto> CreateAsync(int courseId, CreateAssessmentRequest request, CancellationToken ct);

    Task<List<AssessmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct);

   
}


using TmsApi.Application.DTOs;

namespace TmsApi.Application.Interfaces;

public interface IStudentService
{
     Task<StudentResponseDto?> GetByIdAsync (int id, CancellationToken ct);

     Task<StudentResponseDto> CreateAsync (CreateStudentRequest request, CancellationToken ct);

     Task<List<StudentResponseDto>> GetStudent (int id,CancellationToken ct);

}
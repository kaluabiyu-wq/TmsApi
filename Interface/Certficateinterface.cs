
using System.Runtime.ConstrainedExecution;
using Tms.Api.Dtos;
using TmsApi.Entities;
namespace Tms.Api.Services;

public interface ICertficateService
{
    
    Task<CertficateResponseDto?> GetByIdAsync(int courseId,int id,CancellationToken ct);
    Task<CertficateResponseDto?> CreateAsync(int courseId,CreateCertficateRequest request, CancellationToken ct);

    Task<List<CertficateResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct);
}
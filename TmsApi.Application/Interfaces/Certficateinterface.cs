
using System.Runtime.ConstrainedExecution;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Interfaces;
public interface ICertficateService
{
    
    Task<CertficateResponseDto?> GetByIdAsync(int courseId,int id,CancellationToken ct);
    Task<CertficateResponseDto?> CreateAsync(int courseId,CreateCertficateRequest request, CancellationToken ct);

    Task<List<CertficateResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct);
}
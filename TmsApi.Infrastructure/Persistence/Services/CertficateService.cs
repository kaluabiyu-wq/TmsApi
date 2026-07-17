using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace TmsApi.Infrastructure.Persistence.Services;


public class CertificateService(TmSDbContext context, ILogger<CertificateService> logger) : ICertficateService
{

    public Task<CertficateResponseDto?> GetByIdAsync( int courseId,int id, CancellationToken ct) =>
       context.Certificates
       .AsNoTracking()
       .Where(e => e.Id == id && e.CourseId == courseId)
       .Select(e => new CertficateResponseDto(e.Id, e.SerialNumber,e.IssuedAt,e.CourseId, e.StudentId))
       .FirstOrDefaultAsync(ct);

    public  async Task<CertficateResponseDto?> CreateAsync(int courseId,CreateCertficateRequest request, CancellationToken ct)
    {
        var  studentExists = await context.Students.AsNoTracking().AnyAsync(s=>s.ID == request.StudentId,ct);
        if(!studentExists)
        {
            return null;
        }

        var certficates = new Certificate
        {
            SerialNumber = request.SerialNumber,
            IssuedAt = DateTime.UtcNow,
            StudentId = request.StudentId,
            CourseId = courseId,
            
        };

        context.Certificates.Add(certficates);
        await context.SaveChangesAsync(ct);
    
    logger.LogInformation(
        "Certficates {CertficateId} created for student {StudentId} in course {CourseId}",
        certficates.Id,certficates.StudentId,certficates.CourseId);

      return await GetByIdAsync(courseId,certficates.Id,ct);

    }
       
    public async Task<List<CertficateResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        return await context.Certificates
         .AsNoTracking()
       .Where(e => e.CourseId == courseId)
       .Select(e => new CertficateResponseDto(e.Id, e.SerialNumber,e.IssuedAt,e.CourseId, e.StudentId))
       .ToListAsync(ct);
       
    }



}
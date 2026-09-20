
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.Persistence.Services;

public class AssessmentService(TmsDbContext context, ILogger<AssessmentService> logger) : IAssessmentService
{
    public Task<AssessmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        context.Assessments
            .AsNoTracking()
            .Where(a => a.Id == id && a.CourseId == courseId)
            .Select(a => new AssessmentResponseDto(a.Id, a.Title, a.Weight, a.MaxScore, a.CourseId))
            .FirstOrDefaultAsync(ct);

    public async Task<AssessmentResponseDto> CreateAsync(int courseId, CreateAssessmentRequest request, CancellationToken ct)
    {
        var existing = await context.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId && a.Title == request.Title && a.Weight == request.Weight)
            .Select(a => new AssessmentResponseDto(a.Id, a.Title, a.Weight, a.MaxScore, a.CourseId))
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            logger.LogWarning(
                "Duplicate assessment {Title} (weight {Weight}) already exists in course {CourseId} (record {AssessmentId})",
                request.Title, request.Weight, courseId, existing.Id);

            return existing;
        }

        var assessment = new Assessment
        {
            Title = request.Title,
            Weight = request.Weight,
            MaxScore = request.MaxScore,
            CourseId = courseId
        };

        context.Assessments.Add(assessment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Created assessment {Title} (weight {Weight}) record {AssessmentId} in course {CourseId}",
            assessment.Title, assessment.Weight, assessment.Id, assessment.CourseId);

        return (await GetByIdAsync(courseId, assessment.Id, ct))!;
    }

    public async Task<List<AssessmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        return await context.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .Select(a => new AssessmentResponseDto(a.Id, a.Title, a.Weight, a.MaxScore, a.CourseId))
            .ToListAsync(ct);
    }

    
}
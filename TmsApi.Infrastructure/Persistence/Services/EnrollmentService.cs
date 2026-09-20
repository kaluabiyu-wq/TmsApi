using Microsoft.EntityFrameworkCore;

using TmsApi.Domain.Entities;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Enrollments.Queries;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace TmsApi.Infrastructure.Persistence.Services;

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService

{
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.ID == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.ID, e.CourseId, e.StudentId, e.EnrolledAt))
            .FirstOrDefaultAsync(ct);

    public async Task<EnrollmentResponseDto?> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
    {
        var studentExists = await context.Students.AsNoTracking().AnyAsync(s => s.ID == request.StudentId, ct);
        if (!studentExists)
        {
            return null;
        }

        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };

        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "Enrollment {EnrollmentId} created for student {StudentId} in course {CourseId}",
            enrollment.ID, enrollment.StudentId, enrollment.CourseId);

        return await GetByIdAsync(courseId, enrollment.ID, ct);
    }

    public async Task<List<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(e.ID, e.CourseId, e.StudentId, e.EnrolledAt))
            .ToListAsync(ct);
    }

 public Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct) =>
    context.Enrollments
        .AsNoTracking()
        .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);

public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
{
    context.Enrollments.Add(enrollment);
    await context.SaveChangesAsync(ct);
}

public Task<List<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct) =>
    context.Enrollments
        .AsNoTracking()
        .Include(e => e.Course)
        .Where(e => e.StudentId == studentId)
        .ToListAsync(ct);

}
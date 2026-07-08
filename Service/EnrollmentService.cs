

using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;

public class EnrollmentService(TmSDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int
id, CancellationToken ct) =>
context.Enrollments
.AsNoTracking()
.Where(e => e.ID == id && e.CourseId == courseId)
.Select(e => new EnrollmentResponseDto(e.ID, e.CourseId, e.
StudentId, e.EnrolledAt))
.FirstOrDefaultAsync(ct);
public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request, CancellationToken ct)
{
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

    return (await GetByIdAsync(courseId, enrollment.ID, ct))!;

    throw new NotImplementedException();
}

public Task<EnrollmentResponseDto?> GetByCourseAsync(int courseId, CancellationToken ct) =>
context.Enrollments
.AsNoTracking()
.Where(e => e.CourseId == courseId)
.Select(e => new EnrollmentResponseDto(e.ID, e.CourseId, e.
StudentId, e.EnrolledAt))
.FirstOrDefaultAsync(ct);


}

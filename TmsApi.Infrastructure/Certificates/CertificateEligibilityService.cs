using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Certificates;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Certificates;

public sealed class CertificateEligibilityService(TmsDbContext db) : ICertificateEligibilityService
{
    public async Task<CertificateEligibilityResult> CheckAsync(
        int studentId, int courseId, CertificateTemplate template, CancellationToken ct)
    {
        var reasons = new List<string>();

        var enrollment = await db.Enrollments.AsNoTracking()
            .FirstOrDefaultAsync(x => x.StudentId == studentId &&
                                      x.CourseId == courseId && !x.IsArchived, ct);

        if (enrollment is null)
            return new(false, ["The student is not enrolled in this course."]);

        if (!string.Equals(enrollment.Status, "Approved", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(enrollment.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            reasons.Add("The enrollment must be approved.");

        if (template.RequireCompletedEnrollment &&
            !string.Equals(enrollment.Status, "Completed", StringComparison.OrdinalIgnoreCase))
            reasons.Add("The enrollment must be marked Completed before issuing a certificate.");

        if (enrollment.Grade is null)
            reasons.Add("A final course grade has not been recorded.");
        else if (enrollment.Grade.Value < template.MinimumFinalGrade)
            reasons.Add($"The final grade ({enrollment.Grade.Value:0.##}) is below the required {template.MinimumFinalGrade:0.##}.");

        if (template.RequireAllPublishedAssessments)
        {
            var publishedAssessmentIds = await db.Assessments.AsNoTracking()
                .Where(a => a.CourseId == courseId && a.Status == "Published")
                .Select(a => a.Id)
                .ToListAsync(ct);

            if (publishedAssessmentIds.Count > 0)
            {
                var gradedAssessmentIds = await db.GradeRecords.AsNoTracking()
                    .Where(g => g.StudentId == studentId && g.CourseId == courseId &&
                                g.IsPublished && publishedAssessmentIds.Contains(g.AssessmentId))
                    .Select(g => g.AssessmentId)
                    .Distinct()
                    .ToListAsync(ct);

                if (publishedAssessmentIds.Except(gradedAssessmentIds).Any())
                    reasons.Add("The student does not have published results for every published assessment.");
            }
        }

        if (template.MinimumAttendancePercent > 0)
        {
            var attendance = await db.AttendanceRecords.AsNoTracking()
                .Where(a => a.StudentId == studentId && a.Session.CourseId == courseId)
                .Select(a => a.Status)
                .ToListAsync(ct);

            // Excused records are excluded from the denominator, not counted as Present.
            var countable = attendance.Where(s => s != AttendanceStatus.Excused).ToList();
            if (countable.Count == 0)
                reasons.Add("No countable attendance records are available.");
            else
            {
                var attended = countable.Count(s =>
                    s == AttendanceStatus.Present || s == AttendanceStatus.Late);
                var percent = attended * 100m / countable.Count;
                if (percent < template.MinimumAttendancePercent)
                    reasons.Add($"Attendance is {percent:0.##}%; the required minimum is {template.MinimumAttendancePercent:0.##}%.");
            }
        }

        return new(reasons.Count == 0, reasons);
    }
}

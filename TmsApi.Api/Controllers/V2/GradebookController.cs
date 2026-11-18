using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Authorization;
using TmsApi.Api.Contracts;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.Interfaces;
using TmsApi.Application.Notifications;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/gradebook")]
[Authorize]
public sealed class GradebookController(TmsDbContext db, IAuthorizationService authorization, IAuditService audit, INotificationService notifications) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private async Task<Course?> AuthorizedCourse(int courseId, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null) return null;
        var result = await authorization.AuthorizeAsync(User, course, new CourseInstructorRequirement());
        return result.Succeeded ? course : null;
    }

    [HttpGet("courses/{courseId:int}")]
    public async Task<IActionResult> GetCourseGradebook(int courseId, CancellationToken ct)
    {
        var course = await AuthorizedCourse(courseId, ct);
        if (course is null) return NotFound();
        var rows = await db.GradeRecords.AsNoTracking()
            .Where(g => g.CourseId == courseId && g.IsPublished)
            .OrderBy(g => g.Student.Name).ThenBy(g => g.Assessment.Title)
            .Select(g => new GradeRecordDto(g.Id, g.StudentId, g.Student.Name, g.Student.RegistrationNumber,
                g.AssessmentId, g.Assessment.Title, g.Score, g.MaxScore, g.Percentage, g.Weight, g.IsPublished, g.PublishedAt))
            .ToListAsync(ct);
        return Ok(new { course.Id, course.Code, course.Title, Grades = rows });
    }

    /// <summary>Publish grades only from already graded Phase 4 submissions. Never trusts a score from the browser.</summary>
    [HttpPost("courses/{courseId:int}/assessments/{assessmentId:int}/publish")]
    public async Task<IActionResult> PublishAssessmentGrades(int courseId, int assessmentId, CancellationToken ct)
    {
        var course = await AuthorizedCourse(courseId, ct);
        if (course is null) return NotFound();
        var assessment = await db.Assessments.FirstOrDefaultAsync(a => a.Id == assessmentId && a.CourseId == courseId, ct);
        if (assessment is null) return NotFound();
        if (assessment.Status != "Published") return Conflict(new { message = "Publish the assessment before publishing its grades." });
        if (assessment.MaxScore <= 0 || assessment.Weight < 0) return BadRequest(new { message = "Assessment MaxScore and Weight are invalid." });

        var submissions = await db.AssessmentSubmissions
            .Where(s => s.Attempt.AssessmentId == assessmentId && s.GradingStatus == "Graded" && s.Score.HasValue)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new { Submission = s, s.Attempt.StudentId })
            .ToListAsync(ct);
        var latest = submissions.GroupBy(x => x.StudentId).Select(g => g.First()).ToList();
        if (latest.Count == 0) return Conflict(new { message = "No fully graded submissions are available." });

        var auditChanges = new List<(GradeRecord Record, object? OldValues)>();
        foreach (var item in latest)
        {
            var score = item.Submission.Score!.Value;
            if (score < 0 || score > assessment.MaxScore)
                return Conflict(new { message = $"Submission {item.Submission.Id} has a score outside the assessment range." });
            var record = await db.GradeRecords.FirstOrDefaultAsync(g => g.StudentId == item.StudentId && g.AssessmentId == assessmentId, ct);
            if (record is null)
            {
                record = new GradeRecord { StudentId = item.StudentId, CourseId = courseId, AssessmentId = assessmentId };
                db.GradeRecords.Add(record);
            }
            var oldValues = record.Id == 0 ? null : new { record.Score, record.MaxScore, record.Percentage, record.Weight, record.IsPublished };
            record.SubmissionId = item.Submission.Id;
            record.Score = score;
            record.MaxScore = assessment.MaxScore;
            record.Percentage = Math.Round(score / assessment.MaxScore * 100m, 2, MidpointRounding.AwayFromZero);
            record.Weight = assessment.Weight;
            record.IsPublished = true;
            record.PublishedAt = DateTime.UtcNow;
            record.PublishedByUserId = UserId;
            record.UpdatedAt = DateTime.UtcNow;
            auditChanges.Add((record, oldValues));
        }
        await db.SaveChangesAsync(ct);
        foreach (var change in auditChanges)
            await audit.LogGradeChangeAsync(change.Record.Id.ToString(), change.OldValues, new { change.Record.Score, change.Record.MaxScore, change.Record.Percentage, change.Record.Weight, change.Record.IsPublished }, ct);

        var studentIds = latest.Select(x => x.StudentId).Distinct().ToArray();
        var recipients = await db.Students.AsNoTracking().Where(s => studentIds.Contains(s.ID) && s.UserId != null)
            .Select(s => new { s.UserId }).ToListAsync(ct);
        foreach (var recipient in recipients.Where(x => !string.IsNullOrWhiteSpace(x.UserId)))
        {
            await notifications.CreateAsync(new CreateNotificationRequest(
                recipient.UserId!, "Grade published", $"Your result for {assessment.Title} is now available.",
                "Success", "Grades", "/dashboard"), ct);
        }
        return Ok(new { assessmentId, PublishedCount = latest.Count });
    }

    [HttpGet("my-courses")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyGrades(CancellationToken ct)
    {
        var studentId = await db.Students.Where(s => s.UserId == UserId && s.IsActive)
            .Select(s => (int?)s.ID).FirstOrDefaultAsync(ct);
        if (studentId is null) return NotFound();
        var enrollments = await db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == studentId && !e.IsArchived && e.Status == "Approved")
            .Select(e => new { e.CourseId, e.Course.Code, e.Course.Title, e.Grade })
            .ToListAsync(ct);
        var result = new List<StudentCourseGradeDto>();
        foreach (var e in enrollments)
        {
            var grades = await db.GradeRecords.AsNoTracking().Where(g => g.StudentId == studentId && g.CourseId == e.CourseId && g.IsPublished)
                .OrderBy(g => g.Assessment.Title)
                .Select(g => new GradeRecordDto(g.Id, g.StudentId, g.Student.Name, g.Student.RegistrationNumber,
                    g.AssessmentId, g.Assessment.Title, g.Score, g.MaxScore, g.Percentage, g.Weight, g.IsPublished, g.PublishedAt))
                .ToListAsync(ct);
            result.Add(new StudentCourseGradeDto(e.CourseId, e.Code, e.Title, e.Grade, grades));
        }
        return Ok(result);
    }

    [HttpPost("courses/{courseId:int}/calculate-final")]
    public async Task<IActionResult> CalculateFinalGrades(int courseId, CancellationToken ct)
    {
        if (await AuthorizedCourse(courseId, ct) is null) return NotFound();
        var assessments = await db.Assessments.AsNoTracking()
            .Where(a => a.CourseId == courseId && a.Status == "Published")
            .Select(a => new { a.Id, a.Weight }).ToListAsync(ct);
        if (assessments.Count == 0) return Conflict(new { message = "There are no published assessments in this course." });
        if (assessments.Any(a => a.Weight < 0) || assessments.Sum(a => a.Weight) != 100m)
            return Conflict(new { message = "Published assessment weights must total exactly 100 before final grades can be calculated." });

        var enrollments = await db.Enrollments.Where(e => e.CourseId == courseId && e.Status == "Approved" && !e.IsArchived).ToListAsync(ct);
        var updated = 0;
        foreach (var enrollment in enrollments)
        {
            var records = await db.GradeRecords.Where(g => g.CourseId == courseId && g.StudentId == enrollment.StudentId && g.IsPublished)
                .ToListAsync(ct);
            if (assessments.Any(a => records.All(g => g.AssessmentId != a.Id))) continue; // do not treat missing work as zero
            var finalPercent = assessments.Sum(a => records.Single(g => g.AssessmentId == a.Id).Percentage * a.Weight / 100m);
            enrollment.Grade = Math.Round(finalPercent, 2, MidpointRounding.AwayFromZero);
            updated++;
        }
        await db.SaveChangesAsync(ct);
        return Ok(new { courseId, UpdatedEnrollmentGrades = updated, EnrolledStudents = enrollments.Count,
            PendingIncomplete = enrollments.Count - updated });
    }

    [HttpPost("records/{gradeRecordId:int}/appeals")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitAppeal(int gradeRecordId, SubmitGradeAppealRequest request, CancellationToken ct)
    {
        var studentId = await db.Students.Where(s => s.UserId == UserId && s.IsActive).Select(s => (int?)s.ID).FirstOrDefaultAsync(ct);
        if (studentId is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 4000)
            return BadRequest(new { message = "Reason is required and must be at most 4000 characters." });
        var grade = await db.GradeRecords.FirstOrDefaultAsync(g => g.Id == gradeRecordId && g.StudentId == studentId && g.IsPublished, ct);
        if (grade is null) return NotFound();
        if (await db.GradeAppeals.AnyAsync(a => a.GradeRecordId == gradeRecordId && a.Status == "Pending", ct))
            return Conflict(new { message = "A pending appeal already exists for this grade." });
        var appeal = new GradeAppeal { GradeRecordId = gradeRecordId, StudentId = studentId.Value, Reason = request.Reason.Trim() };
        db.GradeAppeals.Add(appeal);
        await db.SaveChangesAsync(ct);
        return Created($"/api/v2/gradebook/appeals/{appeal.Id}", new { appeal.Id, appeal.Status, appeal.SubmittedAt });
    }

    [HttpGet("courses/{courseId:int}/appeals")]
    public async Task<IActionResult> GetAppeals(int courseId, CancellationToken ct)
    {
        if (await AuthorizedCourse(courseId, ct) is null) return NotFound();
        var appeals = await db.GradeAppeals.AsNoTracking().Where(a => a.GradeRecord.CourseId == courseId)
            .OrderBy(a => a.Status).ThenBy(a => a.SubmittedAt)
            .Select(a => new { a.Id, a.GradeRecordId, a.StudentId, StudentName = a.Student.Name,
                a.Reason, a.Status, a.ReviewComment, a.SubmittedAt, a.ReviewedAt })
            .ToListAsync(ct);
        return Ok(appeals);
    }

    [HttpPut("appeals/{appealId:int}/review")]
    public async Task<IActionResult> ReviewAppeal(int appealId, ReviewGradeAppealRequest request, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Instructor")) return Forbid();
        var appeal = await db.GradeAppeals.Include(a => a.GradeRecord).ThenInclude(g => g.Course)
            .FirstOrDefaultAsync(a => a.Id == appealId, ct);
        if (appeal is null) return NotFound();
        var auth = await authorization.AuthorizeAsync(User, appeal.GradeRecord.Course, new CourseInstructorRequirement());
        if (!auth.Succeeded) return Forbid();
        if (appeal.Status != "Pending") return Conflict(new { message = "This appeal has already been reviewed." });
        var decision = request.Decision?.Trim();
        if (decision is not ("Approved" or "Rejected")) return BadRequest(new { message = "Decision must be Approved or Rejected." });
        appeal.Status = decision;
        appeal.ReviewComment = request.Comment?.Trim();
        appeal.ReviewerUserId = UserId;
        appeal.ReviewedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new { appeal.Id, appeal.Status, appeal.ReviewComment, appeal.ReviewedAt });
    }
}

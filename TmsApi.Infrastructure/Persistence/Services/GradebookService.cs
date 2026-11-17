using Microsoft.EntityFrameworkCore;
using TmsApi.Application.DTOs;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Application.Grading.Services;

public class GradebookService(
    TmsDbContext db,
    GradingService grading,
    ICourseAuthorization auth) : IGradebookService
{
    private const int MinTextLength = 10;
    private static readonly string[] ActiveAppealStatuses = ["Pending", "UnderReview"];
    private static readonly string[] ReviewStatuses = ["UnderReview", "Approved", "Rejected"];

    // ───────────────────────── READS ─────────────────────────

    public async Task<CourseGradebookResponse> GetCourseGradebookAsync(
        int courseId, GradebookActor actor, CancellationToken ct)
    {
        await auth.EnsureCanManageCourseAsync(actor, courseId, ct);

        var course = await db.Courses.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId, ct)
            ?? throw new KeyNotFoundException("Course not found.");

        var assessments = await GetCountedAssessmentsAsync(courseId, ct);

        var enrollments = await db.Enrollments.AsNoTracking()
            .Include(e => e.Student)
            .Where(e => e.CourseId == courseId && e.Status == "Approved")
            .ToListAsync(ct);

        var ids = enrollments.Select(e => e.ID).ToList();
        var records = await db.GradeRecords.AsNoTracking()
            .Where(r => ids.Contains(r.EnrollmentId))
            .ToListAsync(ct);

        var students = enrollments
            .Select(e => Project(
                e, course.Title, assessments,
                records.Where(r => r.EnrollmentId == e.ID).ToList(),
                forStudent: false))
            .OrderBy(s => s.StudentName)
            .ToList();

        var total = assessments.Sum(a => a.Weight);
        return new(course.Id, course.Title, total, total == 100m, students);
    }

    public async Task<IReadOnlyList<StudentGradeResponse>> GetMyGradesAsync(
        GradebookActor actor, CancellationToken ct)
    {
        var studentId = await db.Students
            .Where(s => s.UserId == actor.UserId)
            .Select(s => (int?)s.ID)
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("No student profile for this account.");

        return await LoadStudentGradesAsync(studentId, forStudent: true, courseFilter: null, ct);
    }

    public async Task<IReadOnlyList<StudentGradeResponse>> GetStudentGradesAsync(
        int studentId, GradebookActor actor, CancellationToken ct)
    {
        if (actor.IsStudent)
        {
            var own = await db.Students
                .Where(s => s.UserId == actor.UserId)
                .Select(s => (int?)s.ID)
                .FirstOrDefaultAsync(ct);

            if (own != studentId)
                throw new UnauthorizedAccessException("You can only view your own grades.");

            return await LoadStudentGradesAsync(studentId, true, null, ct);
        }

        // Staff: instructors only see enrollments in courses they manage.
        var managed = await auth.GetManagedCourseIdsAsync(actor, ct);
        return await LoadStudentGradesAsync(studentId, false, managed, ct);
    }

    private async Task<IReadOnlyList<StudentGradeResponse>> LoadStudentGradesAsync(
        int studentId, bool forStudent, IReadOnlyList<int>? courseFilter, CancellationToken ct)
    {
        var query = db.Enrollments.AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId && e.Status == "Approved");

        if (courseFilter is not null)
            query = query.Where(e => courseFilter.Contains(e.CourseId));

        var enrollments = await query.ToListAsync(ct);
        if (enrollments.Count == 0) return [];

        var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
        var enrollmentIds = enrollments.Select(e => e.ID).ToList();

        var assessments = await db.Assessments.AsNoTracking()
            .Where(a => courseIds.Contains(a.CourseId) && a.Status == "Published" && a.Weight > 0)
            .OrderBy(a => a.Id)
            .ToListAsync(ct);

        var records = await db.GradeRecords.AsNoTracking()
            .Where(r => enrollmentIds.Contains(r.EnrollmentId))
            .ToListAsync(ct);

        return enrollments
            .Select(e => Project(
                e, e.Course.Title,
                assessments.Where(a => a.CourseId == e.CourseId).ToList(),
                records.Where(r => r.EnrollmentId == e.ID).ToList(),
                forStudent))
            .ToList();
    }

    // Builds one student's view. Students never see drafts or the preview grade.
    private StudentGradeResponse Project(
        Enrollment e, string courseTitle,
        IReadOnlyList<Assessment> assessments,
        IReadOnlyList<GradeRecord> records,
        bool forStudent)
    {
        var items = assessments.Select(a =>
        {
            var r = records.FirstOrDefault(x => x.AssessmentId == a.Id);

            if (r is null || (forStudent && !r.IsPublished))
                return new AssessmentGradeItem(
                    a.Id, a.Title, a.MaxScore, a.Weight,
                    null, null, null, false, null, null);

            return new AssessmentGradeItem(
                a.Id, a.Title, a.MaxScore, a.Weight,
                r.Id, r.Score,
                grading.CalculateWeightedScore(r.Score, r.MaxScore, a.Weight),
                r.IsPublished, r.Feedback, r.GradedAt);
        }).ToList();

        var complete = items.Count > 0 && items.All(i => i.GradeRecordId != null);

        decimal? calculated = null;
        if (!forStudent && complete && assessments.Sum(a => a.Weight) == 100m)
        {
            calculated = grading.CalculateFinalGrade(
                assessments.Select(a =>
                {
                    var r = records.First(x => x.AssessmentId == a.Id);
                    return (r.Score, r.MaxScore, a.Weight);
                }));
        }

        var level = e.Grade is null
            ? "NotPublished"
            : grading.CalculateFromEnrollmentGrade(e.Grade).ToString();

        return new StudentGradeResponse(
            e.StudentId,
            e.Student?.Name ?? "",   // ASSUMPTION: display-name property
            e.ID, e.CourseId, courseTitle,
            e.Grade, calculated, level, complete, items);
    }

    // ─────────────────── ASSESSMENT-LEVEL GRADING ───────────────────

    public Task<GradeRecordResponse> SaveGradeAsync(
        SaveGradeRequest r, GradebookActor actor, CancellationToken ct)
        => UpsertDraftAsync(r.EnrollmentId, r.AssessmentId, r.Score, r.Feedback, actor, ct);

    public async Task<GradeRecordResponse> UpdateGradeAsync(
        int gradeRecordId, UpdateGradeRequest r, GradebookActor actor, CancellationToken ct)
    {
        var existing = await db.GradeRecords.AsNoTracking()
            .Where(x => x.Id == gradeRecordId)
            .Select(x => new { x.EnrollmentId, x.AssessmentId })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Grade record not found.");

        return await UpsertDraftAsync(
            existing.EnrollmentId, existing.AssessmentId, r.Score, r.Feedback, actor, ct);
    }

    private async Task<GradeRecordResponse> UpsertDraftAsync(
        int enrollmentId, int assessmentId, decimal score, string? feedback,
        GradebookActor actor, CancellationToken ct)
    {
        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.ID == enrollmentId, ct)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        await auth.EnsureCanManageCourseAsync(actor, enrollment.CourseId, ct);

        if (enrollment.Status != "Approved")
            throw new InvalidOperationException("Only approved enrollments can be graded.");

        if (enrollment.Grade is not null)
            throw new InvalidOperationException(
                "The final grade is already published. Use the correction process.");

        var assessment = await db.Assessments
            .FirstOrDefaultAsync(a => a.Id == assessmentId && a.CourseId == enrollment.CourseId, ct)
            ?? throw new KeyNotFoundException("Assessment does not belong to this course.");

        if (assessment.Status != "Published")
            throw new InvalidOperationException("Only published assessments can be graded.");

        if (score < 0m || score > assessment.MaxScore)
            throw new ArgumentException(
                $"Score must be between 0 and {assessment.MaxScore}.");

        var record = await db.GradeRecords.FirstOrDefaultAsync(
            r => r.EnrollmentId == enrollmentId && r.AssessmentId == assessmentId, ct);

        if (record?.IsPublished == true)
            throw new InvalidOperationException(
                "This grade is published. Use the correction process.");

        // Authoritative score = highest graded attempt (never averaged).
        var authoritative = await db.AssessmentSubmissions
            .Where(s => s.Attempt.AssessmentId == assessmentId
                     && s.Attempt.StudentId == enrollment.StudentId
                     && s.GradingStatus == "Graded"
                     && s.Score != null)
            .MaxAsync(s => s.Score, ct);

        if (authoritative is null)
            throw new InvalidOperationException(
                "A graded submission is required before a grade record can be saved.");

        var isOverride = score != authoritative.Value;
        if (isOverride && string.IsNullOrWhiteSpace(feedback))
            throw new ArgumentException(
                $"The submission score is {authoritative.Value}. " +
                "Enter a comment to explain a different mark.");

        var weighted = grading.CalculateWeightedScore(score, assessment.MaxScore, assessment.Weight);

        if (record is null)
        {
            record = new GradeRecord
            {
                EnrollmentId = enrollmentId,
                AssessmentId = assessmentId
            };
            db.GradeRecords.Add(record);
        }

        record.Score = score;
        record.MaxScore = assessment.MaxScore;
        record.WeightedScore = weighted;
        record.IsManualOverride = isOverride;
        record.GradedBy = actor.UserId;
        record.GradedAt = DateTime.UtcNow;
        record.Feedback = feedback?.Trim();

        await SaveAsync(ct);

        return ToResponse(record, assessment.Title);
    }

    public async Task PublishAssessmentGradeAsync(
        int gradeRecordId, GradebookActor actor, CancellationToken ct)
    {
        var record = await db.GradeRecords
            .Include(r => r.Enrollment)
            .FirstOrDefaultAsync(r => r.Id == gradeRecordId, ct)
            ?? throw new KeyNotFoundException("Grade record not found.");

        await auth.EnsureCanManageCourseAsync(actor, record.Enrollment.CourseId, ct);

        if (record.IsPublished) return; // idempotent

        record.IsPublished = true;
        record.PublishedAt = DateTime.UtcNow;

        await SaveAsync(ct);
    }

    // ─────────────────── FINAL GRADE ───────────────────

    public async Task<decimal> PublishFinalGradeAsync(
        int enrollmentId, GradebookActor actor, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.ID == enrollmentId, ct)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        await auth.EnsureCanManageCourseAsync(actor, enrollment.CourseId, ct);

        if (enrollment.Status != "Approved")
            throw new InvalidOperationException("Only approved enrollments can receive a final grade.");

        if (enrollment.Grade is not null)
            throw new InvalidOperationException(
                "The final grade is already published. Use the correction process.");

        var assessments = await GetCountedAssessmentsAsync(enrollment.CourseId, ct);
        var records = await db.GradeRecords
            .Where(r => r.EnrollmentId == enrollmentId)
            .ToListAsync(ct);

        var final = ComputeFinal(assessments, records);   // throws if incomplete / bad weights

        var now = DateTime.UtcNow;
        foreach (var r in records.Where(r => !r.IsPublished))
        {
            r.IsPublished = true;
            r.PublishedAt = now;
        }

        enrollment.Grade = final;

        await SaveAsync(ct);
        await tx.CommitAsync(ct);
        return final;
    }

    // Validates weights and completeness; returns the final percentage.
    private decimal ComputeFinal(
        IReadOnlyList<Assessment> assessments, IReadOnlyList<GradeRecord> records)
    {
        if (assessments.Count == 0)
            throw new InvalidOperationException("The course has no published, weighted assessments.");

        grading.ValidateWeights(assessments.Select(a => a.Weight));

        var missing = assessments
            .Where(a => records.All(r => r.AssessmentId != a.Id))
            .Select(a => a.Title)
            .ToList();

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "Missing grades for: " + string.Join(", ", missing) + ".");

        var final = grading.CalculateFinalGrade(
            assessments.Select(a =>
            {
                var r = records.First(x => x.AssessmentId == a.Id);
                return (r.Score, r.MaxScore, a.Weight);
            }));

        if (final is < 0m or > 100m)
            throw new InvalidOperationException("Final grade must be between 0 and 100.");

        return final;
    }

    // ─────────────────── CORRECTIONS ───────────────────

    public async Task<GradeRecordResponse> CorrectGradeAsync(
        int gradeRecordId, CorrectGradeRequest req, GradebookActor actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Reason) || req.Reason.Trim().Length < MinTextLength)
            throw new ArgumentException("A correction reason of at least 10 characters is required.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var record = await db.GradeRecords
            .Include(r => r.Assessment)
            .Include(r => r.Enrollment)
            .FirstOrDefaultAsync(r => r.Id == gradeRecordId, ct)
            ?? throw new KeyNotFoundException("Grade record not found.");

        await auth.EnsureCanManageCourseAsync(actor, record.Enrollment.CourseId, ct);

        if (!record.IsPublished)
            throw new InvalidOperationException("Draft grades can be edited directly; no correction needed.");

        if (req.NewScore < 0m || req.NewScore > record.Assessment.MaxScore)
            throw new ArgumentException(
                $"Score must be between 0 and {record.Assessment.MaxScore}.");

        if (req.AppealId is int appealId)
        {
            var valid = await db.GradeAppeals.AnyAsync(
                a => a.Id == appealId
                  && a.EnrollmentId == record.EnrollmentId
                  && a.Status == "Approved", ct);

            if (!valid)
                throw new InvalidOperationException(
                    "The referenced appeal must be Approved and belong to this enrollment.");
        }

        var previousScore = record.Score;
        var previousFinal = record.Enrollment.Grade;

        record.Score = req.NewScore;
        record.MaxScore = record.Assessment.MaxScore;
        record.WeightedScore = grading.CalculateWeightedScore(
            req.NewScore, record.Assessment.MaxScore, record.Assessment.Weight);
        record.IsManualOverride = true;
        record.GradedBy = actor.UserId;
        record.GradedAt = DateTime.UtcNow;

        // If the official final grade exists, recompute and update it in the same transaction.
        decimal? newFinal = previousFinal;
        if (previousFinal is not null)
        {
            var assessments = await GetCountedAssessmentsAsync(record.Enrollment.CourseId, ct);
            var others = await db.GradeRecords
                .Where(r => r.EnrollmentId == record.EnrollmentId && r.Id != record.Id)
                .ToListAsync(ct);
            others.Add(record);

            newFinal = ComputeFinal(assessments, others);
            record.Enrollment.Grade = newFinal;
        }

        db.GradeChangeHistories.Add(new GradeChangeHistory
        {
            GradeRecordId = record.Id,
            EnrollmentId = record.EnrollmentId,
            AssessmentId = record.AssessmentId,
            PreviousScore = previousScore,
            NewScore = req.NewScore,
            PreviousFinalGrade = previousFinal,
            NewFinalGrade = newFinal,
            Reason = req.Reason.Trim(),
            AppealId = req.AppealId,
            ChangedBy = actor.UserId,
            ChangedAt = DateTime.UtcNow
        });

        await SaveAsync(ct);
        await tx.CommitAsync(ct);

        return ToResponse(record, record.Assessment.Title);
    }

    // ─────────────────── APPEALS ───────────────────

    public async Task<int> SubmitAppealAsync(
        int enrollmentId, SubmitGradeAppealRequest req, GradebookActor actor, CancellationToken ct)
    {
        var reason = req.Reason?.Trim() ?? "";
        if (reason.Length < MinTextLength)
            throw new ArgumentException("Please explain your appeal (at least 10 characters).");
        if (reason.Length > 4000)
            throw new ArgumentException("The reason is too long (maximum 4000 characters).");

        var studentId = await db.Students
            .Where(s => s.UserId == actor.UserId)
            .Select(s => (int?)s.ID)
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("No student profile for this account.");

        var enrollment = await db.Enrollments
            .FirstOrDefaultAsync(e => e.ID == enrollmentId, ct)
            ?? throw new KeyNotFoundException("Enrollment not found.");

        if (enrollment.StudentId != studentId)
            throw new UnauthorizedAccessException("This enrollment does not belong to you.");

        if (enrollment.Grade is null)
            throw new InvalidOperationException("Only published final grades can be appealed.");

        var hasActive = await db.GradeAppeals.AnyAsync(
            a => a.EnrollmentId == enrollmentId && ActiveAppealStatuses.Contains(a.Status), ct);

        if (hasActive)
            throw new InvalidOperationException("You already have an active appeal for this course.");

        var appeal = new GradeAppeal
        {
            EnrollmentId = enrollmentId,
            StudentId = actor.UserId,
            Reason = reason,
            Status = "Pending"
        };

        db.GradeAppeals.Add(appeal);
        await db.SaveChangesAsync(ct);
        return appeal.Id;
    }

    public async Task<IReadOnlyList<GradeAppealResponse>> GetMyAppealsAsync(
        GradebookActor actor, CancellationToken ct)
        => await AppealQuery()
            .Where(a => a.StudentId == actor.UserId)
            .OrderByDescending(a => a.SubmittedAt)
            .Select(ToAppealResponse)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GradeAppealResponse>> GetAppealsAsync(
        string? status, int? courseId, GradebookActor actor, CancellationToken ct)
    {
        var managed = await auth.GetManagedCourseIdsAsync(actor, ct);
        var q = AppealQuery();

        if (managed is not null)
            q = q.Where(a => managed.Contains(a.Enrollment.CourseId));

        if (courseId is int c)
            q = q.Where(a => a.Enrollment.CourseId == c);

        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(a => a.Status == status);

        return await q
            .OrderBy(a => a.SubmittedAt)
            .Select(ToAppealResponse)
            .ToListAsync(ct);
    }

    public async Task ReviewAppealAsync(
        int appealId, ReviewGradeAppealRequest req, GradebookActor actor, CancellationToken ct)
    {
        if (!ReviewStatuses.Contains(req.Status))
            throw new ArgumentException("Status must be UnderReview, Approved or Rejected.");

        var isFinal = req.Status is "Approved" or "Rejected";
        var response = req.Response?.Trim();

        if (isFinal && string.IsNullOrWhiteSpace(response))
            throw new ArgumentException("A response is required when approving or rejecting.");

        var appeal = await db.GradeAppeals
            .Include(a => a.Enrollment)
            .FirstOrDefaultAsync(a => a.Id == appealId, ct)
            ?? throw new KeyNotFoundException("Appeal not found.");

        await auth.EnsureCanManageCourseAsync(actor, appeal.Enrollment.CourseId, ct);

        if (!ActiveAppealStatuses.Contains(appeal.Status))
            throw new InvalidOperationException("This appeal has already been resolved.");

        appeal.Status = req.Status;
        appeal.Response = response;
        appeal.ReviewedBy = actor.UserId;
        appeal.ReviewedAt = DateTime.UtcNow;

        // Approval does NOT change the grade by itself. The official result changes
        // only through POST /gradebook/{id}/correct (with this appeal's id).
        await db.SaveChangesAsync(ct);
    }

    // ─────────────────── HELPERS ───────────────────

    private Task<List<Assessment>> GetCountedAssessmentsAsync(int courseId, CancellationToken ct)
        => db.Assessments.AsNoTracking()
            .Where(a => a.CourseId == courseId && a.Status == "Published" && a.Weight > 0)
            .OrderBy(a => a.Id)
            .ToListAsync(ct);

    private IQueryable<GradeAppeal> AppealQuery()
        => db.GradeAppeals.AsNoTracking()
            .Include(a => a.Enrollment).ThenInclude(e => e.Course)
            .Include(a => a.Enrollment).ThenInclude(e => e.Student);

    private static readonly System.Linq.Expressions.Expression<Func<GradeAppeal, GradeAppealResponse>>
        ToAppealResponse = a => new GradeAppealResponse(
            a.Id, a.EnrollmentId,
            a.Enrollment.Course.Title,
            a.Enrollment.Student.Name,   // ASSUMPTION: display-name property
            a.Reason, a.Status, a.Response, a.SubmittedAt, a.ReviewedAt);

    private static GradeRecordResponse ToResponse(GradeRecord r, string title)
        => new(r.Id, r.EnrollmentId, r.AssessmentId, title,
               r.Score, r.MaxScore, r.WeightedScore,
               r.IsPublished, r.IsManualOverride, r.Feedback, r.GradedAt);

    // Converts EF concurrency / unique-index failures into friendly 409s.
    private async Task SaveAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "This grade was changed by someone else. Refresh and try again.");
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("IX_GradeRecords") == true)
        {
            throw new InvalidOperationException(
                "A grade for this student and assessment already exists.");
        }
    }
}
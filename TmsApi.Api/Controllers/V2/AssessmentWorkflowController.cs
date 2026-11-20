using System.Security.Claims;
using System.Text.Json;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Api.Authorization;
using TmsApi.Api.Contracts;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Api.Notifications;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/courses/{courseId:int}/assessments")]
[Route("api/v{version:apiVersion}/assessments")]
[Authorize]
public sealed class AssessmentWorkflowController(
    TmsDbContext db,
    IAuthorizationService authorization,
    RoleBasedNotificationService roleNotifications) : ControllerBase
{
    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    private async Task<Course?> GetAuthorizedCourse(int courseId, CancellationToken ct)
    {
        var course = await db.Courses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == courseId, ct);
        if (course is null) return null;
        var result = await authorization.AuthorizeAsync(User, course, new CourseInstructorRequirement());
        return result.Succeeded ? course : null;
    }

    private async Task<Assessment?> GetAuthorizedAssessment(int id, CancellationToken ct)
    {
        var assessment = await db.Assessments.Include(x => x.Course).Include(x => x.Questions)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (assessment is null) return null;
        var result = await authorization.AuthorizeAsync(User, assessment.Course, new CourseInstructorRequirement());
        return result.Succeeded ? assessment : null;
    }

    [HttpPost]
    public async Task<IActionResult> Create(int? courseId, [FromBody] CreateAssessmentRequest request, CancellationToken ct)
    {
        var resolvedCourseId = courseId ?? request.CourseId;
        if (resolvedCourseId <= 0) return BadRequest(new { message = "Provide courseId in the route or request body." });
        var course = await GetAuthorizedCourse(resolvedCourseId, ct);
        if (course is null) return NotFound();
        if (request.OpensAt.HasValue && request.ClosesAt.HasValue && request.ClosesAt <= request.OpensAt)
            return BadRequest(new { message = "ClosesAt must be later than OpensAt." });

        var assessment = new Assessment {
            Title = request.Title.Trim(), Description = request.Description?.Trim() ?? "",
            MaxScore = request.MaxScore, Weight = request.Weight, CourseId = course.Id,
            OpensAt = AsUtc(request.OpensAt), ClosesAt = AsUtc(request.ClosesAt),
            DurationMinutes = request.DurationMinutes, MaxAttempts = request.MaxAttempts
        };
        db.Assessments.Add(assessment);
        await db.SaveChangesAsync(ct);
        if (!string.Equals(UserId, course.InstructorId, StringComparison.Ordinal))
            await roleNotifications.NotifyCourseInstructorAsync(course.Id, "New assessment created",
                $"Assessment “{assessment.Title}” was created for {course.Title}.",
                "Assessments", "Info", "/instructor/assessments", ct);
        await roleNotifications.NotifyAdminsAsync("New assessment created",
            $"Assessment “{assessment.Title}” was created for {course.Title}.",
            "Assessments", "Info", "/admin", ct);
        return CreatedAtAction(nameof(Get), new { version = "2.0", courseId = course.Id, id = assessment.Id }, ToInstructorDto(assessment));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int? courseId, int id, [FromBody] UpdateAssessmentRequest request, CancellationToken ct)
    {
        var assessment = await GetAuthorizedAssessment(id, ct);
        if (assessment is null || (courseId.HasValue && assessment.CourseId != courseId.Value)) return NotFound();
        if (assessment.Status == "Published" && await db.AssessmentAttempts.AnyAsync(x => x.AssessmentId == id, ct))
            return Conflict(new { message = "A published assessment with attempts cannot be edited. Create a new assessment instead." });
        if (request.OpensAt.HasValue && request.ClosesAt.HasValue && request.ClosesAt <= request.OpensAt)
            return BadRequest(new { message = "ClosesAt must be later than OpensAt." });
        assessment.Title = request.Title.Trim(); assessment.Description = request.Description?.Trim() ?? "";
        assessment.MaxScore = request.MaxScore; assessment.Weight = request.Weight;
        assessment.OpensAt = AsUtc(request.OpensAt); assessment.ClosesAt = AsUtc(request.ClosesAt);
        assessment.DurationMinutes = request.DurationMinutes; assessment.MaxAttempts = request.MaxAttempts;
        assessment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (!string.Equals(UserId, assessment.Course.InstructorId, StringComparison.Ordinal))
            await roleNotifications.NotifyCourseInstructorAsync(assessment.CourseId, "Assessment updated",
                $"Assessment “{assessment.Title}” was updated.",
                "Assessments", "Info", "/instructor/assessments", ct);
        return Ok(ToInstructorDto(assessment));
    }

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int? courseId, int id, CancellationToken ct)
    {
        var assessment = await GetAuthorizedAssessment(id, ct);
        if (assessment is null || (courseId.HasValue && assessment.CourseId != courseId.Value)) return NotFound();
        if (assessment.Status == "Published") return NoContent();
        if (!assessment.Questions.Any()) return BadRequest(new { message = "Add at least one question before publishing." });
        if (assessment.Questions.Sum(q => q.Points) != assessment.MaxScore)
            return BadRequest(new { message = "The sum of question points must equal MaxScore." });
        if (assessment.OpensAt.HasValue && assessment.ClosesAt.HasValue && assessment.ClosesAt <= assessment.OpensAt)
            return BadRequest(new { message = "ClosesAt must be later than OpensAt." });
        assessment.Status = "Published"; assessment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        if (!string.Equals(UserId, assessment.Course.InstructorId, StringComparison.Ordinal))
            await roleNotifications.NotifyCourseInstructorAsync(assessment.CourseId, "Assessment published",
                $"Assessment “{assessment.Title}” was published.",
                "Assessments", "Info", "/instructor/assessments", ct);
        await roleNotifications.NotifyAssessmentPublishedAsync(assessment.CourseId, assessment.Title, ct);
        await roleNotifications.NotifyAdminsAsync("Assessment published",
            $"Assessment “{assessment.Title}” was published for {assessment.Course.Title}.",
            "Assessments", "Info", "/admin", ct);
        return NoContent();
    }

    [HttpPost("{id:int}/questions")]
    public async Task<IActionResult> AddQuestion(int? courseId, int id, [FromBody] CreateQuestionRequest request, CancellationToken ct)
    {
        var assessment = await GetAuthorizedAssessment(id, ct);
        if (assessment is null || (courseId.HasValue && assessment.CourseId != courseId.Value)) return NotFound();
        if (assessment.Status != "Draft") return Conflict(new { message = "Questions can only be added while the assessment is a draft." });
        var type = request.QuestionType.Trim();
        if (!new[] { "MultipleChoice", "TrueFalse", "ShortAnswer", "Essay" }.Contains(type, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { message = "QuestionType must be MultipleChoice, TrueFalse, ShortAnswer, or Essay." });
        if (type.Equals("MultipleChoice", StringComparison.OrdinalIgnoreCase) && (request.Options is null || request.Options.Count < 2))
            return BadRequest(new { message = "Multiple-choice questions require at least two options." });
        if (!type.Equals("Essay", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(request.CorrectAnswer))
            return BadRequest(new { message = "A correct answer is required for automatically graded questions." });
        if (type.Equals("TrueFalse", StringComparison.OrdinalIgnoreCase) && !new[] { "true", "false" }.Contains(request.CorrectAnswer?.Trim().ToLowerInvariant()))
            return BadRequest(new { message = "TrueFalse correct answer must be true or false." });
        var question = new AssessmentQuestion {
            AssessmentId = id, Prompt = request.Prompt.Trim(), QuestionType = NormalizeType(type),
            OptionsJson = request.Options is null ? null : JsonSerializer.Serialize(request.Options),
            CorrectAnswer = request.CorrectAnswer?.Trim(), Points = request.Points, SortOrder = request.SortOrder
        };
        db.AssessmentQuestions.Add(question);
        await db.SaveChangesAsync(ct);
        return Ok(ToInstructorQuestion(question));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int? courseId, int id, CancellationToken ct)
    {
        var assessment = await db.Assessments.AsNoTracking().Include(x => x.Questions).Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (assessment is null || (courseId.HasValue && assessment.CourseId != courseId.Value)) return NotFound();
        var auth = await authorization.AuthorizeAsync(User, assessment.Course, new CourseInstructorRequirement());
        if (auth.Succeeded) return Ok(ToInstructorDto(assessment));
        if (!User.IsInRole("Student")) return Forbid();
        if (assessment.Status != "Published") return NotFound();
        var studentId = await db.Students.AsNoTracking().Where(s => s.UserId == UserId && s.IsActive).Select(s => (int?)s.ID).FirstOrDefaultAsync(ct);
        if (studentId is null || !await db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == assessment.CourseId && e.Status == "Approved", ct))
            return Forbid();
        return Ok(new {
            assessment.Id, assessment.Title, assessment.Description, assessment.MaxScore, assessment.Weight,
            assessment.OpensAt, assessment.ClosesAt, assessment.DurationMinutes, assessment.MaxAttempts,
            assessment.Status, assessment.CourseId,
            Questions = assessment.Questions.OrderBy(q => q.SortOrder)
                .Select(q => new StudentQuestionDto(q.Id, q.Prompt, q.QuestionType,
                    q.OptionsJson is null ? null : JsonSerializer.Deserialize<List<string>>(q.OptionsJson),
                    q.Points, q.SortOrder))
        });
    }

    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> StartAttempt(int? courseId, int id, CancellationToken ct)
    {
        if (!User.IsInRole("Student")) return Forbid();
        var assessment = await db.Assessments.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (assessment is null || (courseId.HasValue && assessment.CourseId != courseId.Value)) return NotFound();
        if (assessment.Status != "Published") return Conflict(new { message = "Assessment is not published." });
        var now = DateTime.UtcNow;
        if (assessment.OpensAt.HasValue && now < assessment.OpensAt.Value) return Conflict(new { message = "Assessment has not opened yet." });
        if (assessment.ClosesAt.HasValue && now >= assessment.ClosesAt.Value) return Conflict(new { message = "Assessment is closed." });
        var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == UserId && s.IsActive, ct);
        if (student is null) return Forbid();
        var enrolled = await db.Enrollments.AnyAsync(e => e.StudentId == student.ID && e.CourseId == assessment.CourseId && e.Status == "Approved", ct);
        if (!enrolled) return Forbid();
        var attempts = await db.AssessmentAttempts.Where(a => a.AssessmentId == id && a.StudentId == student.ID).OrderBy(a => a.AttemptNumber).ToListAsync(ct);
        var inProgress = attempts.FirstOrDefault(a => a.Status == "InProgress");
        if (inProgress is not null) return Ok(new { inProgress.Id, inProgress.AttemptNumber, inProgress.StartedAt, inProgress.Status });
        if (attempts.Count >= assessment.MaxAttempts) return Conflict(new { message = "Maximum number of attempts reached." });
        var attempt = new AssessmentAttempt { AssessmentId = id, StudentId = student.ID, AttemptNumber = attempts.Count + 1, StartedAt = now };
        db.AssessmentAttempts.Add(attempt);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { return Conflict(new { message = "An attempt could not be created. Refresh and try again." }); }
        return Ok(new { attempt.Id, attempt.AttemptNumber, attempt.StartedAt, attempt.Status });
    }


    [HttpGet("~/api/v{version:apiVersion}/attempts/{attemptId:int}")]
    public async Task<IActionResult> GetAttempt(int attemptId, CancellationToken ct)
    {
        var attempt = await GetOwnedAttempt(attemptId, ct);
        if (attempt is null) return NotFound();
        var answers = await db.AssessmentAnswers.AsNoTracking()
            .Where(a => a.AttemptId == attemptId)
            .Select(a => new { a.QuestionId, a.AnswerText })
            .ToListAsync(ct);
        return Ok(new {
            attempt.Id, attempt.AttemptNumber, attempt.StartedAt, attempt.SubmittedAt, attempt.Status,
            Answers = answers
        });
    }

    [HttpPut("~/api/v{version:apiVersion}/attempts/{attemptId:int}/answers")]
    public async Task<IActionResult> SaveAnswers(int attemptId, [FromBody] SaveAnswersRequest request, CancellationToken ct)
    {
        var attempt = await GetOwnedAttempt(attemptId, ct);
        if (attempt is null) return NotFound();
        var error = ValidateActiveAttempt(attempt);
        if (error is not null) return Conflict(new { message = error });
        var questionIds = attempt.Assessment.Questions.Select(q => q.Id).ToHashSet();
        if (request.Answers.Any(a => !questionIds.Contains(a.QuestionId))) return BadRequest(new { message = "One or more question IDs do not belong to this assessment." });
        foreach (var answer in request.Answers)
        {
            var row = await db.AssessmentAnswers.FirstOrDefaultAsync(a => a.AttemptId == attemptId && a.QuestionId == answer.QuestionId, ct);
            if (row is null) db.AssessmentAnswers.Add(new AssessmentAnswer { AttemptId = attemptId, QuestionId = answer.QuestionId, AnswerText = answer.AnswerText ?? "", UpdatedAt = DateTime.UtcNow });
            else { row.AnswerText = answer.AnswerText ?? ""; row.UpdatedAt = DateTime.UtcNow; }
        }
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("~/api/v{version:apiVersion}/attempts/{attemptId:int}/submit")]
    public async Task<IActionResult> Submit(int attemptId, CancellationToken ct)
    {
        var attempt = await GetOwnedAttempt(attemptId, ct);
        if (attempt is null) return NotFound();
        if (attempt.Submission is not null) return Ok(new { attempt.Submission.Id, attempt.Submission.SubmittedAt, attempt.Submission.Score, attempt.Submission.GradingStatus });
        var error = ValidateActiveAttempt(attempt);
        if (error is not null) return Conflict(new { message = error });
        var answers = await db.AssessmentAnswers.Where(a => a.AttemptId == attemptId).Include(a => a.Question).ToListAsync(ct);
        var answeredQuestionIds = answers.Select(a => a.QuestionId).ToHashSet();
        foreach (var question in attempt.Assessment.Questions.Where(q => !answeredQuestionIds.Contains(q.Id)))
        {
            var blank = new AssessmentAnswer { AttemptId = attemptId, QuestionId = question.Id, AnswerText = "", UpdatedAt = DateTime.UtcNow };
            blank.Question = question;
            db.AssessmentAnswers.Add(blank);
            answers.Add(blank);
        }
        decimal score = 0;
        var manual = false;
        foreach (var answer in answers)
        {
            var q = answer.Question;
            if (q.QuestionType == "Essay") { manual = true; continue; }
            if (!string.IsNullOrWhiteSpace(q.CorrectAnswer) &&
                string.Equals(Normalize(answer.AnswerText), Normalize(q.CorrectAnswer), StringComparison.OrdinalIgnoreCase))
                answer.AwardedPoints = q.Points;
            else answer.AwardedPoints = 0;
            score += answer.AwardedPoints.Value;
        }
        attempt.Status = "Submitted"; attempt.SubmittedAt = DateTime.UtcNow;
        var submission = new AssessmentSubmission {
            AttemptId = attemptId, SubmittedAt = DateTime.UtcNow,
            Score = manual ? null : score, GradingStatus = manual ? "Pending" : "Graded"
        };
        db.AssessmentSubmissions.Add(submission);
        await db.SaveChangesAsync(ct);
        await roleNotifications.NotifyCourseInstructorAsync(attempt.Assessment.CourseId,
            "New assessment submission",
            $"A student submitted “{attempt.Assessment.Title}”.",
            "Assessments", "Info", "/instructor/assessments", ct);
        return Ok(new { submission.Id, submission.SubmittedAt, submission.Score, submission.GradingStatus });
    }

    [HttpGet("{id:int}/submissions")]
    public async Task<IActionResult> GetSubmissions(int? courseId, int id, CancellationToken ct)
    {
        var assessment = await GetAuthorizedAssessment(id, ct);
        if (assessment is null || (courseId.HasValue && assessment.CourseId != courseId.Value)) return NotFound();
        var rows = await db.AssessmentSubmissions.AsNoTracking()
            .Where(s => s.Attempt.AssessmentId == id)
            .OrderByDescending(s => s.SubmittedAt)
            .Select(s => new {
                s.Id, s.AttemptId, s.SubmittedAt, s.Score, s.GradingStatus, s.Feedback,
                StudentId = s.Attempt.StudentId, StudentName = s.Attempt.Student.Name,
                RegistrationNumber = s.Attempt.Student.RegistrationNumber,
                s.Attempt.AttemptNumber,
                Answers = s.Attempt.Answers.OrderBy(a => a.Question.SortOrder).Select(a => new {
                    a.QuestionId, a.Question.Prompt, a.Question.QuestionType, a.AnswerText,
                    a.Question.CorrectAnswer, a.Question.Points, a.AwardedPoints
                })
            }).ToListAsync(ct);
        return Ok(rows);
    }


    [HttpPut("{id:int}/submissions/{submissionId:int}/grade")]
    public async Task<IActionResult> GradeSubmission(int? courseId, int id, int submissionId, [FromBody] GradeSubmissionRequest request, CancellationToken ct)
    {
        var assessment = await GetAuthorizedAssessment(id, ct);
        if (assessment is null || (courseId.HasValue && assessment.CourseId != courseId.Value)) return NotFound();
        var submission = await db.AssessmentSubmissions.Include(s => s.Attempt).ThenInclude(a => a.Answers)
            .ThenInclude(a => a.Question)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.Attempt.AssessmentId == id, ct);
        if (submission is null) return NotFound();
        var answersByQuestion = submission.Attempt.Answers.ToDictionary(a => a.QuestionId);
        foreach (var grade in request.Answers)
        {
            if (!answersByQuestion.TryGetValue(grade.QuestionId, out var answer))
                return BadRequest(new { message = $"Question {grade.QuestionId} is not part of this submission." });
            if (grade.AwardedPoints < 0 || grade.AwardedPoints > answer.Question.Points)
                return BadRequest(new { message = $"Awarded points for question {grade.QuestionId} must be between 0 and {answer.Question.Points}." });
            answer.AwardedPoints = grade.AwardedPoints;
        }
        submission.Feedback = request.Feedback;
        if (submission.Attempt.Answers.All(a => a.AwardedPoints.HasValue))
        {
            submission.Score = submission.Attempt.Answers.Sum(a => a.AwardedPoints!.Value);
            submission.GradingStatus = "Graded";
        }
        else submission.GradingStatus = "Pending";
        await db.SaveChangesAsync(ct);
        if (!string.Equals(UserId, assessment.Course.InstructorId, StringComparison.Ordinal))
            await roleNotifications.NotifyCourseInstructorAsync(assessment.CourseId, "Assessment grading updated",
                $"Grading was updated for “{assessment.Title}”.",
                "Assessments", "Info", "/instructor/assessments", ct);
        await roleNotifications.NotifyAssessmentGradedAsync(submission.Attempt.StudentId,
            assessment.Title, submission.Score, ct);
        return Ok(new { submission.Id, submission.Score, submission.GradingStatus, submission.Feedback });
    }

    private async Task<AssessmentAttempt?> GetOwnedAttempt(int id, CancellationToken ct)
    {
        if (!User.IsInRole("Student")) return null;
        var studentId = await db.Students.AsNoTracking().Where(s => s.UserId == UserId && s.IsActive).Select(s => (int?)s.ID).FirstOrDefaultAsync(ct);
        if (studentId is null) return null;
        return await db.AssessmentAttempts.Include(a => a.Assessment).ThenInclude(a => a.Questions)
            .Include(a => a.Submission).FirstOrDefaultAsync(a => a.Id == id && a.StudentId == studentId, ct);
    }

    private static string? ValidateActiveAttempt(AssessmentAttempt attempt)
    {
        if (attempt.Status != "InProgress") return "This attempt is no longer active.";
        var deadline = attempt.StartedAt.AddMinutes(attempt.Assessment.DurationMinutes);
        if (attempt.Assessment.ClosesAt.HasValue && attempt.Assessment.ClosesAt.Value < deadline) deadline = attempt.Assessment.ClosesAt.Value;
        if (DateTime.UtcNow > deadline) return "The allowed examination time has expired.";
        return null;
    }
    private static DateTime? AsUtc(DateTime? value) => value.HasValue
        ? (value.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value.Value.ToUniversalTime())
        : null;
    private static string Normalize(string? value) => System.Text.RegularExpressions.Regex.Replace((value ?? "").Trim(), @"\s+", " ");
    private static string NormalizeType(string type) => type.ToLowerInvariant() switch {
        "multiplechoice" => "MultipleChoice", "truefalse" => "TrueFalse",
        "shortanswer" => "ShortAnswer", _ => "Essay"
    };
    private static object ToInstructorDto(Assessment a) => new {
        a.Id, a.Title, a.Description, a.MaxScore, a.Weight, a.CourseId, a.OpensAt, a.ClosesAt,
        a.DurationMinutes, a.MaxAttempts, a.Status, a.CreatedAt, a.UpdatedAt,
        Questions = a.Questions.OrderBy(q => q.SortOrder).Select(ToInstructorQuestion)
    };
    private static InstructorQuestionDto ToInstructorQuestion(AssessmentQuestion q) =>
        new(q.Id, q.Prompt, q.QuestionType,
            q.OptionsJson is null ? null : JsonSerializer.Deserialize<List<string>>(q.OptionsJson),
            q.CorrectAnswer, q.Points, q.SortOrder);
}

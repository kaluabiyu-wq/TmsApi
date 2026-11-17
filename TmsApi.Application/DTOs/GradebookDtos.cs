namespace TmsApi.Application.DTOs;


public sealed record GradebookActor(string UserId, bool IsAdmin, bool IsStudent);

public sealed record SaveGradeRequest(
    int EnrollmentId, int AssessmentId, decimal Score, string? Feedback);

public sealed record UpdateGradeRequest(decimal Score, string? Feedback);

public sealed record CorrectGradeRequest(
    decimal NewScore, string Reason, int? AppealId);

public sealed record GradeRecordResponse(
    int Id, int EnrollmentId, int AssessmentId, string AssessmentTitle,
    decimal Score, decimal MaxScore, decimal WeightedScore,
    bool IsPublished, bool IsManualOverride, string? Feedback, DateTime GradedAt);

// One row per COUNTED assessment. Missing/unpublished => GradeRecordId,
// Score, WeightedScore are null (never zero).
public sealed record AssessmentGradeItem(
    int AssessmentId, string AssessmentTitle, decimal MaxScore, decimal Weight,
    int? GradeRecordId, decimal? Score, decimal? WeightedScore,
    bool IsPublished, string? Feedback, DateTime? GradedAt);

public sealed record StudentGradeResponse(
    int StudentId, string StudentName, int EnrollmentId,
    int CourseId, string CourseTitle,
    decimal? FinalGrade,        // Enrollment.Grade (published only)
    decimal? CalculatedGrade,   // staff-only preview; null when incomplete
    string GradeLevel,          // "NotPublished" until final grade published
    bool IsComplete,
    IReadOnlyList<AssessmentGradeItem> Assessments);

public sealed record CourseGradebookResponse(
    int CourseId, string CourseTitle,
    decimal TotalWeight, bool WeightsValid,
    IReadOnlyList<StudentGradeResponse> Students);

public sealed record SubmitGradeAppealRequest(string Reason);

public sealed record ReviewGradeAppealRequest(string Status, string? Response);

public sealed record GradeAppealResponse(
    int Id, int EnrollmentId, string CourseTitle, string StudentName,
    string Reason, string Status, string? Response,
    DateTime SubmittedAt, DateTime? ReviewedAt);
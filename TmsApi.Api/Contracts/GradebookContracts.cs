namespace TmsApi.Api.Contracts;

public sealed record SubmitGradeAppealRequest(string Reason);
public sealed record ReviewGradeAppealRequest(string Decision, string? Comment);
public sealed record GradeRecordDto(
    int Id, int StudentId, string StudentName, string RegistrationNumber,
    int AssessmentId, string AssessmentTitle, decimal Score, decimal MaxScore,
    decimal Percentage, decimal Weight, bool IsPublished, DateTime? PublishedAt);
public sealed record StudentCourseGradeDto(
    int CourseId, string CourseCode, string CourseTitle, decimal? FinalGrade,
    IReadOnlyList<GradeRecordDto> AssessmentGrades);

using System.ComponentModel.DataAnnotations;

namespace TmsApi.Api.Contracts;

public sealed record CreateAssessmentRequest(
    [Required, StringLength(200, MinimumLength = 3)] string Title,
    [StringLength(4000)] string? Description,
    [Range(typeof(decimal), "0.01", "1000000")] decimal MaxScore,
    [Range(typeof(decimal), "0", "100")] decimal Weight,
    DateTime? OpensAt,
    DateTime? ClosesAt,
    [Range(1, 600)] int DurationMinutes = 60,
    [Range(1, 20)] int MaxAttempts = 1,
    int CourseId = 0);

public sealed record UpdateAssessmentRequest(
    [Required, StringLength(200, MinimumLength = 3)] string Title,
    [StringLength(4000)] string? Description,
    [Range(typeof(decimal), "0.01", "1000000")] decimal MaxScore,
    [Range(typeof(decimal), "0", "100")] decimal Weight,
    DateTime? OpensAt,
    DateTime? ClosesAt,
    [Range(1, 600)] int DurationMinutes,
    [Range(1, 20)] int MaxAttempts);

public sealed record CreateQuestionRequest(
    [Required, StringLength(4000, MinimumLength = 1)] string Prompt,
    [Required] string QuestionType,
    IReadOnlyList<string>? Options,
    string? CorrectAnswer,
    [Range(typeof(decimal), "0.01", "1000000")] decimal Points,
    int SortOrder = 0);

public sealed record SaveAnswerRequest(int QuestionId, [StringLength(8000)] string AnswerText);
public sealed record SaveAnswersRequest(IReadOnlyList<SaveAnswerRequest> Answers);
public sealed record StudentQuestionDto(int Id, string Prompt, string QuestionType, IReadOnlyList<string>? Options, decimal Points, int SortOrder);
public sealed record InstructorQuestionDto(int Id, string Prompt, string QuestionType, IReadOnlyList<string>? Options, string? CorrectAnswer, decimal Points, int SortOrder);

public sealed record GradeAnswerRequest(int QuestionId, decimal AwardedPoints);
public sealed record GradeSubmissionRequest(IReadOnlyList<GradeAnswerRequest> Answers, string? Feedback);

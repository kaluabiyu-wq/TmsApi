namespace TmsApi.Domain.Entities;
public class AssessmentAnswer
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public AssessmentAttempt Attempt { get; set; } = null!;
    public int QuestionId { get; set; }
    public AssessmentQuestion Question { get; set; } = null!;
    public string AnswerText { get; set; } = string.Empty;
    public decimal? AwardedPoints { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

namespace TmsApi.Domain.Entities;
public class AssessmentSubmission
{
    public int Id { get; set; }
    public int AttemptId { get; set; }
    public AssessmentAttempt Attempt { get; set; } = null!;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public decimal? Score { get; set; }
    public string GradingStatus { get; set; } = "Pending";
    public string? Feedback { get; set; }
}

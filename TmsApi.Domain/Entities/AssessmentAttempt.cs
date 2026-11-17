namespace TmsApi.Domain.Entities;
public class AssessmentAttempt
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int AttemptNumber { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = "InProgress";
    public AssessmentSubmission? Submission { get; set; }
    public ICollection<AssessmentAnswer> Answers { get; set; } = new List<AssessmentAnswer>();
}

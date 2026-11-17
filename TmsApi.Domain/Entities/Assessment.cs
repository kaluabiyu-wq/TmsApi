namespace TmsApi.Domain.Entities;
public class Assessment
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public DateTime? OpensAt { get; set; }
    public DateTime? ClosesAt { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public int MaxAttempts { get; set; } = 1;
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<AssessmentQuestion> Questions { get; set; } = new List<AssessmentQuestion>();
    public ICollection<AssessmentAttempt> Attempts { get; set; } = new List<AssessmentAttempt>();
}

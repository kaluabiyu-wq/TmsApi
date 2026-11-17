namespace TmsApi.Domain.Entities;

/// <summary>Published assessment result for one student. Score is the raw score on MaxScore.</summary>
public sealed class GradeRecord
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = null!;
    public int SubmissionId { get; set; }
    public AssessmentSubmission Submission { get; set; } = null!;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Percentage { get; set; }
    public decimal Weight { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<GradeAppeal> Appeals { get; set; } = new List<GradeAppeal>();
}

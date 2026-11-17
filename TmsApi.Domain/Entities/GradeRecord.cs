namespace TmsApi.Domain.Entities;

public class GradeRecord
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    public int AssessmentId { get; set; }
    public Assessment Assessment { get; set; } = null!;

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }

    public decimal WeightedScore { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }

    public bool IsManualOverride { get; set; }

    public string GradedBy { get; set; } = "";
    public DateTime GradedAt { get; set; } = DateTime.UtcNow;
    public string? Feedback { get; set; }
}
namespace TmsApi.Domain.Entities;

public class GradeChangeHistory
{
    public int Id { get; set; }

    public int GradeRecordId { get; set; }
    public int EnrollmentId { get; set; }
    public int AssessmentId { get; set; }

    public decimal PreviousScore { get; set; }
    public decimal NewScore { get; set; }
    public decimal? PreviousFinalGrade { get; set; }
    public decimal? NewFinalGrade { get; set; }

    public string Reason { get; set; } = "";
    public int? AppealId { get; set; }

    public string ChangedBy { get; set; } = "";
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
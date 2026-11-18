namespace TmsApi.Domain.Entities;

public sealed class GradeAppeal
{
    public int Id { get; set; }
    public int GradeRecordId { get; set; }
    public GradeRecord GradeRecord { get; set; } = null!;
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string? ReviewerUserId { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}

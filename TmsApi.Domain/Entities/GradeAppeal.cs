namespace TmsApi.Domain.Entities;

public class GradeAppeal
{
    public int Id { get; set; }

    public int EnrollmentId { get; set; }
    public Enrollment Enrollment { get; set; } = null!;

    // Identity user id (string), not Student.ID.
    public string StudentId { get; set; } = "";

    public string Reason { get; set; } = "";

    // Pending | UnderReview | Approved | Rejected
    public string Status { get; set; } = "Pending";

    public string? Response { get; set; }
    public string? ReviewedBy { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
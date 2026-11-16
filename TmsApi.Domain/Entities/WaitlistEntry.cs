namespace TmsApi.Domain.Entities;

public static class WaitlistStatus
{
    public const string Waiting = "Waiting";     // in the queue
    public const string Offered = "Offered";     // a seat is being held for the student
    public const string Accepted = "Accepted";   // student took the seat -> Enrollment created/approved
    public const string Cancelled = "Cancelled"; // student (or staff) left the queue / declined the offer
    public const string Expired = "Expired";     // offer window passed without acceptance
}

public class WaitlistEntry
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int Position { get; set; }

    public string Status { get; set; } = WaitlistStatus.Waiting;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? OfferedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }

    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
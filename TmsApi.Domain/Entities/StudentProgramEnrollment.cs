namespace TmsApi.Domain.Entities;

public sealed class StudentProgramEnrollment
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int ProgramId { get; set; }
    public TrainingProgram Program { get; set; } = null!;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    /// <summary>Active, Completed or Withdrawn.</summary>
    public string Status { get; set; } = "Active";
    public DateTime? CompletedAt { get; set; }
    public DateTime? WithdrawnAt { get; set; }
    public ICollection<StudentProgress> ProgressItems { get; set; } = new List<StudentProgress>();
}

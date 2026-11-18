namespace TmsApi.Domain.Entities;

/// <summary>Links a catalogue course to a program; the course itself is never duplicated.</summary>
public sealed class ProgramCourse
{
    public int Id { get; set; }
    public int ProgramId { get; set; }
    public TrainingProgram Program { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public int Sequence { get; set; }
    public bool IsRequired { get; set; } = true;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public ICollection<StudentProgress> StudentProgress { get; set; } = new List<StudentProgress>();
}

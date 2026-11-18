namespace TmsApi.Domain.Entities;

/// <summary>A reusable institutional pathway made up of existing catalogue courses.</summary>
public sealed class TrainingProgram
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? DurationMonths { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ProgramCourse> ProgramCourses { get; set; } = new List<ProgramCourse>();
    public ICollection<StudentProgramEnrollment> StudentEnrollments { get; set; } = new List<StudentProgramEnrollment>();
}

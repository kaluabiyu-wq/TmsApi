
namespace TmsApi.Domain.Entities;

public class Student

{
    public int ID { get; set;}

    public required string RegistrationNumber { get; set;}

    public required string Name {get; set ;}

    // Identity account linked by an administrator for secure student exam ownership.
    public string? UserId { get; set; }

    public decimal GPA { get; set;}
    public bool IsActive { get; set; } =true;
   
   public bool IsDeleted {get;set;} = false;
   public uint Version { get; set; }
    public ICollection<AssessmentAttempt> AssessmentAttempts { get; set; } = new List<AssessmentAttempt>();
    public ICollection<Enrollment> Enrollments { get; set;} = new List<Enrollment>();
    public ICollection<Certificate> Certificates { get; set;} = new List<Certificate>();

}
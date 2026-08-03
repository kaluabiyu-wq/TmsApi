

namespace TmsApi.Domain.Entities;

public class Enrollment

{
    public int ID { get; set;}

    public int StudentId {get; set;}
    public int CourseId {get; set;}

    public decimal? Grade { get; set;}

    public int Year {get; set;}
    

   public bool IsArchived {get;set;} = false;
    public DateTime EnrolledAt { get; set;} = DateTime.UtcNow;
    public Student Student { get; set;} = null!;
    public Course Course {get; set;} = null!;

}
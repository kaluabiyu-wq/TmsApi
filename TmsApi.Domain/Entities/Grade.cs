namespace TmsApi.Domain.Entities;


public class Grade
{
    public int Id {get;set;}
     public int StudentId {get; set;}
    public int CourseId {get; set;}
    public int Score {get;set;}

    public Student Student { get; set;} = null!;
    public Course Course {get; set;} = null!;


}
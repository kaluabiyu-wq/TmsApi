namespace TmsApi.Application.DTOs;

public record CreateStudentRequest
{

    public required string RegistrationNumber { get; set;}

    public required string Name {get; set ;}

    public decimal GPA { get; set;}
    public bool IsActive { get; set; } =true;
   

}
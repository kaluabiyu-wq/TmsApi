using FluentValidation;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentValidator : AbstractValidator<EnrollstudentCommand>
{
    
   public EnrollStudentValidator()
    {
        RuleFor(x => x.StudentId).GreaterThan(0)
        .WithMessage("Student ID must be a positive number.");
        RuleFor(x => x.Coursecode).NotEmpty()
        .WithMessage("Course code is required.");
        RuleFor(x=>x.Coursecode).Matches(@"^[A-z]{3}-\d{3}$")
        .WithMessage("Course Code must follow the format xxx-000 (e.g., CSE-101).");
        
    }


}
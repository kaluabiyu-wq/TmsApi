using MediatR;
using TmsApi.Application.Common;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Enrollments.Commands;

public class EnrollStudentHandler(IEnrollmentService enrollmentService,
 ICourseService courseService
):
IRequestHandler<EnrollstudentCommand, Result<EnrollmentCreated,EnrollmentError>>
{
    public async Task<Result<EnrollmentCreated, EnrollmentError>> Handle
    (
        EnrollstudentCommand command, CancellationToken ct)
    {
        var course = await courseService.GetByCodeAsync(command.Coursecode,ct);
        if(course is null)
        return Result<EnrollmentCreated, EnrollmentError>.Failure(
            EnrollmentError.courseNotFound(command.Coursecode));

       if(course.EnrollmentCount >= course.MaxCapacity)
         return Result<EnrollmentCreated, EnrollmentError>.Failure(
            EnrollmentError.courseFull(course.Title,course.MaxCapacity));

            
      if(await enrollmentService.ExistsAsync(command.StudentId,command.Coursecode, ct))
      return Result<EnrollmentCreated, EnrollmentError>.Failure(
          EnrollmentError.AlreadyEnrolled(command.StudentId,command.Coursecode));
     var enrollment = new Enrollment
     {
         StudentId = command.StudentId,
         CourseId = course.Id,
         EnrolledAt = DateTime.UtcNow
     };

     await enrollmentService.AddAsync(enrollment,ct);
     return Result<EnrollmentCreated, EnrollmentError>.Success(
        new EnrollmentCreated(enrollment.ID,enrollment.StudentId,course.Code));
    

    } 

}
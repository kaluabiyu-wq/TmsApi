

using System.Security.Cryptography.X509Certificates;
using NSubstitute;
using TmsApi.Application.Common;
using TmsApi.Application.DTOs;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Interfaces;
using TmsApi.Domain.Entities;

namespace TmsApi_Tests;

public class EnrollStudentStudentHandlerTests
{
    [Fact]
    public async Task Handle_WhenAlreadyEnrolled_ReturnDuplicate()
    {
          var enrollmentService = Substitute.For<IEnrollmentService>();
          var courseService = Substitute.For<ICourseService>();

          enrollmentService.ExistsAsync(99, "Cs-401", Arg.Any<CancellationToken>())
          .Returns(Task.FromResult(true));

          var course = new Course
          {
              Id = 1,
              Code = "Cs-401",
              Title = "Advanced Web Dev",
              MaxCapacity = 30,
               Enrollments = new List<Enrollment>(),
          };
          courseService.GetCourseByCodeAsync("Cs-401", Arg.Any<CancellationToken>())
          .Returns(Task.FromResult<Course?>(course));

          var handler = new EnrollStudentHandler(enrollmentService, courseService);
          var command = new EnrollstudentCommand(StudentId: 99,Coursecode: "Cs-401");
          var result = await handler.Handle(command, CancellationToken.None);

          Assert.False(result.IsSuccess);
          Assert.Equal("already_enrolled", result.Error.Code);
          Assert.Equal(EnrollmentError.AlreadyEnrolled(99, "CS-401"),result.Error);

          await enrollmentService.DidNotReceive()
          .AddAsync(Arg.Any<Enrollment>(), Arg.Any<CancellationToken>());

        
    }

    [Fact]
    public async Task Handle_WhenCourseFull_ReturnsCapacityError()
    {
        var enrollmentService = Substitute.For<IEnrollmentService>();
        var courseService = Substitute.For<ICourseService>();

        var course = new Course
        {
            Id = 1,
            Code = "CS-401",
            Title = "Advanced web dev",
            MaxCapacity = 35,
            Enrollments = Enumerable.Range(1,35)
            .Select(i=> new Enrollment {ID = i, CourseId = 1, Status = "pending"})
            .ToList()
        };
        courseService.GetCourseByCodeAsync("CS-401", Arg.Any<CancellationToken>())
        .Returns(Task.FromResult<Course?>(course));

        var handler = new EnrollStudentHandler(enrollmentService, courseService);
        var command = new EnrollstudentCommand(StudentId:100, Coursecode: "CS-401");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("course_full", result.Error.Code);
        Assert.Equal(EnrollmentError.courseFull("Advanced web Dev",35), result.Error);

        await enrollmentService.DidNotReceive()
        .AddAsync(Arg.Any<Enrollment>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_SuccessfulPath_AddsEnrollmentOnce()
    {
        var enrollmentService = Substitute.For<IEnrollmentService>();
        var courseService = Substitute.For<ICourseService>();


         var course = new Course
        {
            Id = 1,
            Code = "CS-401",
            Title = "Advanced web dev",
            MaxCapacity = 35,
            Enrollments = Enumerable.Range(1,20)
            .Select(i=> new Enrollment {ID = i, CourseId = 1, Status = "pending"})
            .ToList()
        };
        courseService.GetCourseByCodeAsync("CS-401",Arg.Any<CancellationToken>())
        .Returns(Task.FromResult<Course?>(course));
        enrollmentService.ExistsAsync(100, "CS-401",Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(false));
        var handler = new EnrollStudentHandler(enrollmentService,courseService);
        var command = new EnrollstudentCommand(StudentId: 100, Coursecode: "CS-401");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.StudentId);
        Assert.Equal("Cs-401", result.Value.CourseCode);

        await enrollmentService.Received(1).AddAsync(
            Arg.Is<Enrollment>(e=>e.StudentId == 100 && e.CourseId == 1),
            Arg.Any<CancellationToken>());
        
    }
}
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

          enrollmentService.ExistsAsync(99, "CS-401", Arg.Any<CancellationToken>())
          .Returns(Task.FromResult(true));

          var course = new CourseResponseDto(
              Id: 1,
              Code: "CS-401",
              Title: "Advanced Web Dev",
              MaxCapacity: 30,
              EnrollmentCount: 0);
          courseService.GetByCodeAsync("CS-401", Arg.Any<CancellationToken>())
          .Returns(Task.FromResult<CourseResponseDto?>(course));

          var handler = new EnrollStudentHandler(enrollmentService, courseService);
          var command = new EnrollstudentCommand(StudentId: 99, Coursecode: "CS-401");
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

         var course = new CourseResponseDto(
            Id: 1,
            Code: "CS-401",
            Title: "Advanced web dev",
            MaxCapacity: 35,
            EnrollmentCount: 35);
        courseService.GetByCodeAsync("CS-401", Arg.Any<CancellationToken>())
        .Returns(Task.FromResult<CourseResponseDto?>(course));

        var handler = new EnrollStudentHandler(enrollmentService, courseService);
        var command = new EnrollstudentCommand(StudentId:100, Coursecode: "CS-401");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("course_full", result.Error.Code);
        Assert.Equal(EnrollmentError.courseFull("Advanced web dev",35), result.Error);

        await enrollmentService.DidNotReceive()
        .AddAsync(Arg.Any<Enrollment>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_SuccessfulPath_AddsEnrollmentOnce()
    {
        var enrollmentService = Substitute.For<IEnrollmentService>();
        var courseService = Substitute.For<ICourseService>();

         var course = new CourseResponseDto(
            Id: 1,
            Code: "CS-401",
            Title: "Advanced web dev",
            MaxCapacity: 35,
            EnrollmentCount: 20);
        courseService.GetByCodeAsync("CS-401",Arg.Any<CancellationToken>())
        .Returns(Task.FromResult<CourseResponseDto?>(course));
        enrollmentService.ExistsAsync(100, "CS-401",Arg.Any<CancellationToken>())
        .Returns(Task.FromResult(false));
        var handler = new EnrollStudentHandler(enrollmentService,courseService);
        var command = new EnrollstudentCommand(StudentId: 100, Coursecode: "CS-401");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.StudentId);
        Assert.Equal("CS-401", result.Value.CourseCode);

        await enrollmentService.Received(1).AddAsync(
            Arg.Is<Enrollment>(e=>e.StudentId == 100 && e.CourseId == 1),
            Arg.Any<CancellationToken>());
        
    }
}
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using TmsApi.Application.Programs;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Infrastructure.Programs;

namespace TmsApi_Tests;

public sealed class ProgramProgressServiceTests
{
    [Fact]
    public async Task RecalculateProgress_UsesCompletedCourseEnrollment_NotManualPercentage()
    {
        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseInMemoryDatabase($"program-progress-{Guid.NewGuid()}").Options;
        await using var db = new TmsDbContext(options);
        var student = new Student { RegistrationNumber = "ST-001", Name = "Test Student" };
        var course = new Course { Code = "WEB-101", Title = "HTML and CSS", MaxCapacity = 30 };
        var program = new TrainingProgram { Code = "WEB-PROG", Name = "Web Development" };
        db.AddRange(student, course, program);
        await db.SaveChangesAsync();
        var programCourse = new ProgramCourse { ProgramId = program.Id, CourseId = course.Id, Sequence = 1, IsRequired = true };
        var programEnrollment = new StudentProgramEnrollment { StudentId = student.ID, ProgramId = program.Id };
        db.AddRange(programCourse, programEnrollment);
        await db.SaveChangesAsync();
        db.Enrollments.Add(new Enrollment { StudentId = student.ID, CourseId = course.Id, Year = DateTime.UtcNow.Year, Status = "Completed" });
        await db.SaveChangesAsync();

        var service = new ProgressService(db);
        var result = await service.RecalculateProgressAsync(programEnrollment.Id);

        Assert.NotNull(result);
        Assert.Equal(100m, result.CompletionPercent);
        Assert.True(result.IsEligibleForCompletion);
        Assert.Equal("Completed", result.Courses.Single().Status);
        Assert.Equal("Completed", result.EnrollmentStatus);
    }

    [Fact]
    public async Task RegisterStudent_RejectsDuplicateActiveProgramEnrollment()
    {
        var options = new DbContextOptionsBuilder<TmsDbContext>()
            .UseInMemoryDatabase($"program-duplicate-{Guid.NewGuid()}").Options;
        await using var db = new TmsDbContext(options);
        var student = new Student { RegistrationNumber = "ST-002", Name = "Another Student" };
        var course = new Course { Code = "WEB-102", Title = "JavaScript", MaxCapacity = 20 };
        var program = new TrainingProgram { Code = "WEB-PROG-2", Name = "Web Pathway" };
        db.AddRange(student, course, program);
        await db.SaveChangesAsync();
        db.ProgramCourses.Add(new ProgramCourse { ProgramId = program.Id, CourseId = course.Id, Sequence = 1 });
        db.StudentProgramEnrollments.Add(new StudentProgramEnrollment { StudentId = student.ID, ProgramId = program.Id });
        await db.SaveChangesAsync();

        var service = new ProgramService(db, Substitute.For<IProgressService>());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterStudentAsync(program.Id, student.ID));
    }
}

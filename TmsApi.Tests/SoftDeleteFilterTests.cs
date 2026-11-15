using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi_Tests;

public class SoftDeleteFilterTests : IClassFixture<CustomWebApplicatonFactory>
{
    private readonly CustomWebApplicatonFactory _factory;

    public SoftDeleteFilterTests(CustomWebApplicatonFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enrollments_OfSoftDeletedStudent_AreHiddenByDefault_ButKeptInDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var course = new Course { Code = $"SDF-{suffix}", Title = "Soft delete course", MaxCapacity = 5 };
        var student = new Student { RegistrationNumber = $"SDF-{suffix}", Name = "To Be Deleted" };
        db.Courses.Add(course);
        db.Students.Add(student);
        await db.SaveChangesAsync();

        db.Enrollments.Add(new Enrollment { StudentId = student.ID, CourseId = course.Id, Year = 2026 });
        await db.SaveChangesAsync();

        // Visible while the student is active.
        Assert.Equal(1, await db.Enrollments.CountAsync(e => e.CourseId == course.Id));

        student.IsDeleted = true;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Hidden by default once the student is soft-deleted...
        Assert.Equal(0, await db.Enrollments.CountAsync(e => e.CourseId == course.Id));

        // ...but the row is still in the database for audit.
        Assert.Equal(1, await db.Enrollments.IgnoreQueryFilters().CountAsync(e => e.CourseId == course.Id));
    }
}

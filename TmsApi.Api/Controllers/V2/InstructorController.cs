using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

/// <summary>
/// Instructor portal read endpoints. Strictly Instructor-only (an Admin does NOT
/// pass this controller) and every query is filtered by the caller's own
/// Identity user id, so an instructor can never see another instructor's data.
/// Writing endpoints (attendance, assessments, grading, schedules) stay in their
/// existing controllers, which already enforce course ownership.
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/instructor")]
[Authorize(Roles = "Instructor")]
public sealed class InstructorController(TmsDbContext db, UserManager<TmsUser> userManager) : ControllerBase
{
    private string? InstructorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

    public sealed record InstructorCourseDto(int Id, string Code, string Title, string Description,
        int MaxCapacity, int ApprovedStudents, int PendingStudents, int AssessmentCount);

    public sealed record InstructorStudentDto(int StudentId, string Name, string RegistrationNumber,
        int CourseId, string CourseCode, string CourseTitle, string EnrollmentStatus, decimal? Grade);

    public sealed record InstructorAssessmentDto(int Id, int CourseId, string CourseCode, string Title,
        string Status, decimal MaxScore, decimal Weight, DateTime? OpensAt, DateTime? ClosesAt, int QuestionCount);

    public sealed record InstructorProfileDto(string Id, string Email, string FirstName, string LastName, string? Department);

    public sealed record UpcomingClassDto(int ScheduleId, int CourseId, string CourseCode, string CourseTitle,
        DateTime StartAt, DateTime EndAt, string ClassroomName, string? Topic);

    public sealed record InstructorDashboardDto(int CourseCount, int StudentCount, int PendingEnrollments,
        int DraftAssessments, int PublishedAssessments, int UpcomingClassCount, IReadOnlyList<UpcomingClassDto> NextClasses);

    [HttpGet("dashboard")]
    public async Task<ActionResult<InstructorDashboardDto>> Dashboard(CancellationToken ct)
    {
        var id = InstructorId;
        if (string.IsNullOrEmpty(id)) return Unauthorized();

        var courseIds = db.Courses.AsNoTracking().Where(c => c.InstructorId == id).Select(c => c.Id);

        var courseCount = await courseIds.CountAsync(ct);

        var studentCount = await db.Enrollments.AsNoTracking()
            .Where(e => courseIds.Contains(e.CourseId) && !e.IsArchived && e.Status == "Approved")
            .Select(e => e.StudentId).Distinct().CountAsync(ct);

        var pending = await db.Enrollments.AsNoTracking()
            .CountAsync(e => courseIds.Contains(e.CourseId) && !e.IsArchived && e.Status == "Pending", ct);

        var drafts = await db.Assessments.AsNoTracking()
            .CountAsync(a => courseIds.Contains(a.CourseId) && a.Status == "Draft", ct);
        var published = await db.Assessments.AsNoTracking()
            .CountAsync(a => courseIds.Contains(a.CourseId) && a.Status != "Draft", ct);

        var now = DateTime.UtcNow;
        var upcomingQuery = db.CourseSchedules.AsNoTracking()
            .Where(s => s.InstructorId == id && !s.IsCancelled && s.EndAt >= now);

        var upcomingCount = await upcomingQuery.CountAsync(ct);
        var next = await upcomingQuery
            .OrderBy(s => s.StartAt).Take(5)
            .Select(s => new UpcomingClassDto(s.Id, s.CourseId, s.Course.Code, s.Course.Title,
                s.StartAt, s.EndAt, s.Classroom.Name, s.Topic))
            .ToListAsync(ct);

        return new InstructorDashboardDto(courseCount, studentCount, pending, drafts, published, upcomingCount, next);
    }

    [HttpGet("courses")]
    public async Task<ActionResult<IReadOnlyList<InstructorCourseDto>>> Courses(CancellationToken ct)
    {
        var id = InstructorId;
        if (string.IsNullOrEmpty(id)) return Unauthorized();

        var rows = await db.Courses.AsNoTracking()
            .Where(c => c.InstructorId == id)
            .OrderBy(c => c.Code)
            .Select(c => new InstructorCourseDto(
                c.Id, c.Code, c.Title, c.Description, c.MaxCapacity,
                c.Enrollments.Count(e => !e.IsArchived && e.Status == "Approved"),
                c.Enrollments.Count(e => !e.IsArchived && e.Status == "Pending"),
                c.Assessments.Count))
            .ToListAsync(ct);

        return rows;
    }

    [HttpGet("students")]
    public async Task<ActionResult<IReadOnlyList<InstructorStudentDto>>> Students([FromQuery] int? courseId, CancellationToken ct)
    {
        var id = InstructorId;
        if (string.IsNullOrEmpty(id)) return Unauthorized();

        var query = db.Enrollments.AsNoTracking()
            .Where(e => e.Course.InstructorId == id && !e.IsArchived);

        if (courseId is not null)
        {
            query = query.Where(e => e.CourseId == courseId);
        }

        var rows = await query
            .OrderBy(e => e.Course.Code).ThenBy(e => e.Student.Name)
            .Select(e => new InstructorStudentDto(e.StudentId, e.Student.Name, e.Student.RegistrationNumber,
                e.CourseId, e.Course.Code, e.Course.Title, e.Status, e.Grade))
            .ToListAsync(ct);

        return rows;
    }

    [HttpGet("assessments")]
    public async Task<ActionResult<IReadOnlyList<InstructorAssessmentDto>>> Assessments([FromQuery] int? courseId, CancellationToken ct)
    {
        var id = InstructorId;
        if (string.IsNullOrEmpty(id)) return Unauthorized();

        var query = db.Assessments.AsNoTracking().Where(a => a.Course.InstructorId == id);
        if (courseId is not null)
        {
            query = query.Where(a => a.CourseId == courseId);
        }

        var rows = await query
            .OrderBy(a => a.Course.Code).ThenBy(a => a.Title)
            .Select(a => new InstructorAssessmentDto(a.Id, a.CourseId, a.Course.Code, a.Title, a.Status,
                a.MaxScore, a.Weight, a.OpensAt, a.ClosesAt, a.Questions.Count))
            .ToListAsync(ct);

        return rows;
    }

    [HttpGet("profile")]
    public async Task<ActionResult<InstructorProfileDto>> Profile()
    {
        var id = InstructorId;
        if (string.IsNullOrEmpty(id)) return Unauthorized();

        var user = await userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        return new InstructorProfileDto(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.Department);
    }
}

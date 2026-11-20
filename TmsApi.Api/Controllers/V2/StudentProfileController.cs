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
/// The signed-in student's own full profile. Student-only; the student record is found
/// from the caller's Identity id, so a student can never request someone else's profile.
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/student/profile")]
[Authorize(Roles = "Student")]
public sealed class StudentProfileController(TmsDbContext db, UserManager<TmsUser> userManager) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";

    public sealed record AccountDto(string UserId, string Email, bool EmailConfirmed, string FirstName,
        string LastName, string FullName, string? PhoneNumber);

    public sealed record StudentRecordDto(int Id, string RegistrationNumber, string Name, decimal Gpa,
        string Status, bool IsActive);

    public sealed record ProfileEnrollmentDto(int EnrollmentId, int CourseId, string CourseCode, string CourseTitle,
        string? InstructorName, int Year, string Status, decimal? Grade, DateTime EnrolledAt);

    public sealed record ProfileCertificateDto(int Id, string SerialNumber, string CourseCode, string CourseTitle,
        string Status, DateTime IssuedAt);

    public sealed record ProfileProgramDto(int ProgramId, string Code, string Name, string Status, DateTime EnrolledAt,
        DateTime? CompletedAt);

    public sealed record AttendanceSummaryDto(int Present, int Late, int Excused, int Absent, int Total, double? AttendanceRate);

    public sealed record AcademicSummaryDto(int TotalCourses, int ApprovedCourses, int PendingCourses,
        int GradedCourses, decimal? AverageGrade, int Certificates);

    public sealed record StudentFullProfileDto(AccountDto Account, StudentRecordDto Student, AcademicSummaryDto Summary,
        AttendanceSummaryDto Attendance, IReadOnlyList<ProfileEnrollmentDto> Enrollments,
        IReadOnlyList<ProfileCertificateDto> Certificates, IReadOnlyList<ProfileProgramDto> Programs);

    [HttpGet]
    public async Task<ActionResult<StudentFullProfileDto>> Get(CancellationToken ct)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return Unauthorized();

        var student = await db.Students.AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, ct);
        if (student is null)
        {
            return NotFound(new { detail = "No student record is linked to your account yet. Please contact an administrator." });
        }

        var enrollmentRows = await db.Enrollments.AsNoTracking()
            .Where(e => e.StudentId == student.ID && !e.IsArchived)
            .OrderByDescending(e => e.Year).ThenBy(e => e.Course.Code)
            .Select(e => new
            {
                e.ID, e.CourseId, e.Course.Code, e.Course.Title, e.Course.InstructorId,
                e.Year, e.Status, e.Grade, e.EnrolledAt
            })
            .ToListAsync(ct);

        var instructorIds = enrollmentRows
            .Select(e => e.InstructorId).Where(i => !string.IsNullOrEmpty(i)).Distinct().ToList();
        var instructorNames = await userManager.Users
            .Where(u => instructorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FirstName, u.LastName })
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), ct);

        var enrollments = enrollmentRows.Select(e => new ProfileEnrollmentDto(
            e.ID, e.CourseId, e.Code, e.Title,
            !string.IsNullOrEmpty(e.InstructorId) && instructorNames.TryGetValue(e.InstructorId, out var n) ? n : null,
            e.Year, e.Status, e.Grade, e.EnrolledAt)).ToList();

        var certificates = await db.Certificates.AsNoTracking()
            .Where(c => c.StudentId == student.ID)
            .OrderByDescending(c => c.IssuedAt)
            .Select(c => new ProfileCertificateDto(c.Id, c.SerialNumber, c.Course.Code, c.Course.Title, c.Status, c.IssuedAt))
            .ToListAsync(ct);

        var programs = await db.StudentProgramEnrollments.AsNoTracking()
            .Where(p => p.StudentId == student.ID)
            .OrderByDescending(p => p.EnrolledAt)
            .Select(p => new ProfileProgramDto(p.ProgramId, p.Program.Code, p.Program.Name, p.Status, p.EnrolledAt, p.CompletedAt))
            .ToListAsync(ct);

        var statuses = await db.AttendanceRecords.AsNoTracking()
            .Where(a => a.StudentId == student.ID)
            .Select(a => a.Status)
            .ToListAsync(ct);
        var present = statuses.Count(s => s == AttendanceStatus.Present);
        var late = statuses.Count(s => s == AttendanceStatus.Late);
        var excused = statuses.Count(s => s == AttendanceStatus.Excused);
        var absent = statuses.Count(s => s == AttendanceStatus.Absent);
        var counted = statuses.Count - excused; // excused absences do not hurt the rate
        double? rate = counted > 0 ? Math.Round((present + late) * 100.0 / counted, 1) : null;

        var graded = enrollments.Where(e => e.Grade is not null).ToList();
        decimal? average = graded.Count > 0 ? Math.Round(graded.Average(e => e.Grade!.Value), 1) : null;

        return new StudentFullProfileDto(
            new AccountDto(user.Id, user.Email ?? string.Empty, user.EmailConfirmed, user.FirstName, user.LastName,
                $"{user.FirstName} {user.LastName}".Trim(), user.PhoneNumber),
            new StudentRecordDto(student.ID, student.RegistrationNumber, student.Name, student.GPA, student.Status, student.IsActive),
            new AcademicSummaryDto(enrollments.Count, enrollments.Count(e => e.Status == "Approved"),
                enrollments.Count(e => e.Status == "Pending"), graded.Count, average,
                certificates.Count(c => c.Status == "Issued")),
            new AttendanceSummaryDto(present, late, excused, absent, statuses.Count, rate),
            enrollments, certificates, programs);
    }
}

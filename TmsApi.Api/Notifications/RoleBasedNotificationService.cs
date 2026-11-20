using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Notifications;
using TmsApi.Application.Scheduling;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Notifications;

/// <summary>
/// Resolves notification recipients from server-side role and course relationships.
/// Callers provide business identifiers, never recipient user IDs supplied by clients.
/// </summary>
public sealed class RoleBasedNotificationService(
    TmsDbContext db,
    UserManager<TmsUser> users,
    INotificationService notifications)
{
    public async Task NotifyRoleAsync(string role, string title, string message, string category,
        string type = "Info", string? actionUrl = null, CancellationToken ct = default)
    {
        var recipients = await users.GetUsersInRoleAsync(role);
        await NotifyUsersAsync(recipients.Select(x => x.Id), title, message, category, type, actionUrl, ct);
    }

    public async Task NotifyAdminsAsync(string title, string message, string category,
        string type = "Info", string? actionUrl = "/admin", CancellationToken ct = default)
    {
        var recipients = new List<TmsUser>();
        recipients.AddRange(await users.GetUsersInRoleAsync("Admin"));
        recipients.AddRange(await users.GetUsersInRoleAsync("SuperAdmin"));
        await NotifyUsersAsync(recipients.Select(x => x.Id), title, message, category, type, actionUrl, ct);
    }

    public async Task NotifyCourseInstructorAsync(int courseId, string title, string message,
        string category, string type = "Info", string? actionUrl = "/instructor", CancellationToken ct = default)
    {
        var instructorId = await db.Courses.AsNoTracking()
            .Where(x => x.Id == courseId).Select(x => x.InstructorId).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrWhiteSpace(instructorId))
            await NotifyUsersAsync([instructorId], title, message, category, type, actionUrl, ct);
    }

    public async Task NotifyCourseStudentsAsync(int courseId, string title, string message,
        string category, string type = "Info", string? actionUrl = "/dashboard", CancellationToken ct = default)
    {
        var studentUsers = await db.Enrollments.AsNoTracking()
            .Where(x => x.CourseId == courseId && !x.IsArchived && x.Status == "Approved" && x.Student.UserId != null)
            .Select(x => x.Student.UserId!)
            .Distinct().ToListAsync(ct);
        await NotifyUsersAsync(studentUsers, title, message, category, type, actionUrl, ct);
    }

    public async Task NotifyEnrollmentSubmittedAsync(int enrollmentId, CancellationToken ct = default)
    {
        var item = await db.Enrollments.AsNoTracking()
            .Include(x => x.Course).Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.ID == enrollmentId, ct);
        if (item is null) return;

        var message = $"{item.Student.Name} submitted an enrollment request for {item.Course.Title}.";
        if (!string.IsNullOrWhiteSpace(item.Student.UserId))
            await NotifyUsersAsync([item.Student.UserId], "Enrollment request received",
                $"Your enrollment request for {item.Course.Title} has been submitted and is pending review.",
                "Enrollment", "Info", "/dashboard", ct);

        if (!string.IsNullOrWhiteSpace(item.Course.InstructorId))
            await NotifyUsersAsync([item.Course.InstructorId], "New student enrollment request",
                message, "Enrollment", "Info", "/instructor", ct);

        await NotifyAdminsAsync("New enrollment request", message, "Enrollment", "Info", "/admin", ct);
    }

    public async Task NotifyEnrollmentDecisionAsync(int enrollmentId, string status, CancellationToken ct = default)
    {
        var item = await db.Enrollments.AsNoTracking()
            .Include(x => x.Course).Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.ID == enrollmentId, ct);
        if (item is null || string.IsNullOrWhiteSpace(item.Student.UserId)) return;

        var approved = string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);
        var title = approved ? "Enrollment approved" : "Enrollment rejected";
        var message = approved
            ? $"Your enrollment in {item.Course.Title} has been approved."
            : $"Your enrollment request for {item.Course.Title} has been rejected.";
        await NotifyUsersAsync([item.Student.UserId], title, message, "Enrollment",
            approved ? "Success" : "Warning", "/dashboard", ct);
        await NotifyAdminsAsync(title, $"{item.Student.Name}: {item.Course.Title}.", "Enrollment",
            approved ? "Success" : "Warning", "/admin", ct);
    }

    public async Task NotifyCourseAssignmentChangedAsync(int courseId, string? previousInstructorId,
        string newInstructorId, CancellationToken ct = default)
    {
        var courseTitle = await db.Courses.AsNoTracking().Where(x => x.Id == courseId)
            .Select(x => x.Title).FirstOrDefaultAsync(ct) ?? "a course";
        if (!string.IsNullOrWhiteSpace(previousInstructorId) &&
            !string.Equals(previousInstructorId, newInstructorId, StringComparison.Ordinal))
        {
            await NotifyUsersAsync([previousInstructorId], "Course assignment changed",
                $"You are no longer assigned to {courseTitle}.",
                "CourseAssignments", "Warning", "/instructor/courses", ct);
        }
        await NotifyUsersAsync([newInstructorId], "New course assigned",
            $"You have been assigned to {courseTitle}.",
            "CourseAssignments", "Success", "/instructor/courses", ct);
        await NotifyAdminsAsync("Course instructor assignment updated",
            $"{courseTitle} was assigned to a new instructor.",
            "CourseAssignments", "Info", "/admin/courses", ct);
    }

    public async Task NotifyScheduleChangedAsync(CourseScheduleResponse schedule, bool created,
        CancellationToken ct = default)
    {
        var title = schedule.IsCancelled ? "Class cancelled" : created ? "New class scheduled" : "Class schedule updated";
        var message = schedule.IsCancelled
            ? $"{schedule.CourseTitle}: the class scheduled for {schedule.StartAt.ToString("g")} has been cancelled."
            : $"{schedule.CourseTitle}: {schedule.StartAt.ToString("g")}–{schedule.EndAt.ToString("t")} in {schedule.ClassroomName}.";
        if (!string.IsNullOrWhiteSpace(schedule.InstructorId))
            await NotifyUsersAsync([schedule.InstructorId], title, message, "Timetable", "Info", "/instructor/timetable", ct);
        await NotifyCourseStudentsAsync(schedule.CourseId, title, message, "Timetable", "Info", "/timetable", ct);
        await NotifyAdminsAsync(title, message, "Timetable", "Info", "/admin", ct);
    }

    public async Task NotifyAssessmentPublishedAsync(int courseId, string assessmentTitle, CancellationToken ct = default)
    {
        var courseTitle = await db.Courses.AsNoTracking().Where(x => x.Id == courseId)
            .Select(x => x.Title).FirstOrDefaultAsync(ct) ?? "your course";
        await NotifyCourseStudentsAsync(courseId, "New assessment published",
            $"The assessment “{assessmentTitle}” is now available for {courseTitle}.",
            "Assessments", "Info", "/assessments", ct);
    }

    public async Task NotifyAssessmentGradedAsync(int studentId, string assessmentTitle, decimal? score, CancellationToken ct = default)
    {
        var userId = await db.Students.AsNoTracking().Where(x => x.ID == studentId)
            .Select(x => x.UserId).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(userId)) return;
        var result = score.HasValue ? $" Your recorded score is {score.Value}." : string.Empty;
        await NotifyUsersAsync([userId], "Assessment result updated",
            $"Your submission for “{assessmentTitle}” has been graded.{result}",
            "Assessments", "Success", "/assessments/results", ct);
    }

    public async Task NotifyUsersAsync(IEnumerable<string?> userIds, string title, string message,
        string category, string type = "Info", string? actionUrl = null, CancellationToken ct = default)
    {
        foreach (var id in userIds.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!)
                     .Distinct(StringComparer.Ordinal))
        {
            await notifications.CreateAsync(new CreateNotificationRequest(
                id, title, message, type, category, actionUrl), ct);
        }
    }
}

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TmsApi.Infrastructure.Persistence;
using TmsApi.Application.Hubs;

namespace TmsApi.Api.Hubs;

[Authorize]
public class TmsHub(TmsDbContext db) : Hub<ITmsHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var studentId = Context.GetHttpContext()?.Request.Query["studentId"].ToString();
        var authenticatedUserId = Context.UserIdentifier;
        if (!string.IsNullOrWhiteSpace(studentId) && !string.IsNullOrWhiteSpace(authenticatedUserId) &&
            int.TryParse(studentId, out var parsedStudentId) &&
            await db.Students.AsNoTracking().AnyAsync(s => s.ID == parsedStudentId && s.UserId == authenticatedUserId))
        {
            // A client may request a student group, but may only join the group linked to its own JWT identity.
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Student(studentId));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinCourseGroup(string courseCode)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNames.Course(courseCode));
    }

    public async Task LeaveCourseGroup(string courseCode)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNames.Course(courseCode));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // SignalR removes the connection from all groups automatically.
        await base.OnDisconnectedAsync(exception);
    }
}

public static class GroupNames
{
    public static string Student(string studentId) => $"student-{studentId}";
    public static string Course(string courseCode) => $"course-{courseCode}";
}

using Microsoft.AspNetCore.SignalR;

namespace TmsApi.Api.Hubs;

public class WaitlistHub : Hub
{
    public async Task JoinStudentGroup(int studentId)
    {
        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            $"student-{studentId}");
    }

    public async Task LeaveStudentGroup(int studentId)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            $"student-{studentId}");
    }
}
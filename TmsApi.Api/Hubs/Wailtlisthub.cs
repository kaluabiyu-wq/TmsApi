using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace TmsApi.Api.Hubs;

[Authorize]
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
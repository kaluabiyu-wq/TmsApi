using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.Waitlists;

namespace TmsApi.Api.Services;

public class WaitlistNotifier : IWaitlistNotifier
{
    private readonly IHubContext<WaitlistHub> _hub;

    public WaitlistNotifier(IHubContext<WaitlistHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyOfferedAsync(WaitlistResponse offer)
    {
        await _hub.Clients
            .Group($"student-{offer.StudentId}")
            .SendAsync("WaitlistOfferReceived", offer);
    }

    public async Task NotifyOfferExpiredAsync(
        int studentId,
        int courseId,
        string courseTitle)
    {
        await _hub.Clients
            .Group($"student-{studentId}")
            .SendAsync("WaitlistOfferExpired", new
            {
                studentId,
                courseId,
                courseTitle
            });
    }
}
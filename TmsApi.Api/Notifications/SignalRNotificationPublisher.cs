using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.Notifications;
using TmsApi.Application.Hubs;

namespace TmsApi.Api.Notifications;

public sealed class SignalRNotificationPublisher(IHubContext<TmsHub, ITmsHubClient> hub) : INotificationRealtimePublisher
{
    public Task PublishAsync(string userId, NotificationRealtimeMessage message, CancellationToken ct = default)
    {
        // Clients.User maps to the authenticated NameIdentifier claim; never trust a client-supplied user group.
        return hub.Clients.User(userId).ReceiveNotification(message);
    }
}

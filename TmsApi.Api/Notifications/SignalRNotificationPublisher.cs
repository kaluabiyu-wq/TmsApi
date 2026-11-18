
using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.Hubs;
using TmsApi.Application.Notifications;

namespace TmsApi.Api.Notifications;

public sealed class SignalRNotificationPublisher(
    IHubContext<TmsHub, ITmsHubClient> hubContext)
    : INotificationRealtimePublisher
{
    public async Task PublishAsync(
        string userId,
        NotificationRealtimeMessage notification,
        CancellationToken cancellationToken = default)
    {
        await hubContext.Clients
            .User(userId)
            .ReceiveNotification(notification);
    }
}

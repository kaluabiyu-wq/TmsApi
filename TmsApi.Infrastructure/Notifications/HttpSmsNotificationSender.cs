using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TmsApi.Application.Notifications;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Notifications;

/// <summary>Generic HTTP SMS adapter. Configure it to match the SMS provider's JSON contract.</summary>
public sealed class HttpSmsNotificationSender(IHttpClientFactory clients, IOptions<SmsNotificationOptions> options) : INotificationChannelSender
{
    public string Channel => "Sms";

    public async Task SendAsync(Notification notification, TmsUser user, CancellationToken ct)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.Endpoint)) throw new InvalidOperationException("SMS endpoint is not configured.");
        if (string.IsNullOrWhiteSpace(user.PhoneNumber)) throw new InvalidOperationException("Recipient has no phone number.");
        var client = clients.CreateClient("TmsSms");
        if (!string.IsNullOrWhiteSpace(config.ApiKey)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);
        using var response = await client.PostAsJsonAsync(config.Endpoint,
            new { to = user.PhoneNumber, from = config.SenderName, message = notification.Title + ": " + notification.Message }, ct);
        response.EnsureSuccessStatusCode();
    }
}

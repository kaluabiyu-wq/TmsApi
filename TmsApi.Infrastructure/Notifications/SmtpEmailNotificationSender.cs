using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TmsApi.Application.Notifications;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Notifications;

public sealed class SmtpEmailNotificationSender(IOptions<SmtpNotificationOptions> options) : INotificationChannelSender
{
    public string Channel => "Email";

    public async Task SendAsync(Notification notification, TmsUser user, CancellationToken ct)
    {
        var config = options.Value;
        if (string.IsNullOrWhiteSpace(config.Host) || string.IsNullOrWhiteSpace(config.FromAddress))
            throw new InvalidOperationException("SMTP is not configured. Set Notifications:Smtp:Host and FromAddress.");
        if (string.IsNullOrWhiteSpace(user.Email)) throw new InvalidOperationException("Recipient has no email address.");
        using var mail = new MailMessage
        {
            From = new MailAddress(config.FromAddress, config.FromName),
            Subject = notification.Title,
            Body = notification.Message + (string.IsNullOrWhiteSpace(notification.ActionUrl) ? "" : $"\n\nOpen: {notification.ActionUrl}"),
            IsBodyHtml = false
        };
        mail.To.Add(user.Email);
        using var client = new SmtpClient(config.Host, config.Port) { EnableSsl = config.EnableSsl };
        if (!string.IsNullOrWhiteSpace(config.Username)) client.Credentials = new NetworkCredential(config.Username, config.Password);
        ct.ThrowIfCancellationRequested();
        await client.SendMailAsync(mail, ct);
    }
}

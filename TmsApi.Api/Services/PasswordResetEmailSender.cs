using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TmsApi.Infrastructure.Notifications;

namespace TmsApi.Api.Services;

public interface IPasswordResetEmailSender
{
    Task SendAsync(string toEmail, string firstName, string resetLink, CancellationToken ct);
}

/// <summary>
/// Sends the password-reset email through the same SMTP settings as the notification
/// system (Notifications:Smtp). When SMTP is not configured and the app runs in
/// Development, the link is written to the console log instead so the flow can be tested.
/// </summary>
public sealed class PasswordResetEmailSender(
    IOptions<SmtpNotificationOptions> options,
    IHostEnvironment environment,
    ILogger<PasswordResetEmailSender> logger) : IPasswordResetEmailSender
{
    public async Task SendAsync(string toEmail, string firstName, string resetLink, CancellationToken ct)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(config.Host) || string.IsNullOrWhiteSpace(config.FromAddress))
        {
            if (environment.IsDevelopment())
            {
                logger.LogWarning("SMTP not configured. DEV ONLY - password reset link for {Email}: {Link}", toEmail, resetLink);
                return;
            }
            throw new InvalidOperationException("SMTP is not configured. Set Notifications:Smtp:Host and FromAddress.");
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(config.FromAddress, config.FromName),
            Subject = "Reset your TMS password",
            Body = $"Hello {firstName},\n\n" +
                   "We received a request to reset your TMS password. Open the link below to choose a new one:\n\n" +
                   $"{resetLink}\n\n" +
                   "The link expires soon and can be used once. If you did not ask for this, ignore this email - your password stays the same.",
            IsBodyHtml = false
        };
        mail.To.Add(toEmail);

        using var client = new SmtpClient(config.Host, config.Port) { EnableSsl = config.EnableSsl };
        if (!string.IsNullOrWhiteSpace(config.Username))
            client.Credentials = new NetworkCredential(config.Username, config.Password);

        await client.SendMailAsync(mail, ct);
    }
}

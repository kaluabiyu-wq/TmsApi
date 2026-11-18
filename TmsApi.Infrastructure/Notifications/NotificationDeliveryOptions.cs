namespace TmsApi.Infrastructure.Notifications;

public sealed class SmtpNotificationOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "TMS";
}

public sealed class SmsNotificationOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string SenderName { get; set; } = "TMS";
}

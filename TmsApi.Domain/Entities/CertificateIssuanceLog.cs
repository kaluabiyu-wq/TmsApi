namespace TmsApi.Domain.Entities;

public class CertificateIssuanceLog
{
    public long Id { get; set; }
    public int CertificateId { get; set; }
    public string Action { get; set; } = string.Empty; // Issued, Downloaded, Verified, Revoked
    public string PerformedBy { get; set; } = "Anonymous";
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Certificate Certificate { get; set; } = null!;
}

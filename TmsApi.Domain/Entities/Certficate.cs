namespace TmsApi.Domain.Entities;

public class Certificate
{
    public int Id { get; set; }
    public required string SerialNumber { get; set; }
    public string VerificationCode { get; set; } = string.Empty;
    public string Status { get; set; } = "Issued"; // Issued, Revoked
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public string IssuedBy { get; set; } = string.Empty;
    public string? CertificateUrl { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public DateTime? RevokedAt { get; set; }
    public string? RevocationReason { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public int? TemplateId { get; set; }
    public Student Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public CertificateTemplate? Template { get; set; }
    public ICollection<CertificateIssuanceLog> IssuanceLogs { get; set; } = new List<CertificateIssuanceLog>();
}

namespace TmsApi.Domain.Entities;

public class CertificateTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "Default certificate";
    public string InstitutionName { get; set; } = "Training Institution";
    public string? LogoPath { get; set; }
    public string? SignatureName { get; set; }
    public string? SignatureTitle { get; set; }
    public string CertificateTitle { get; set; } = "CERTIFICATE OF COMPLETION";
    public string BodyText { get; set; } = "This is to certify that";
    public decimal MinimumFinalGrade { get; set; } = 50m;
    public decimal MinimumAttendancePercent { get; set; } = 0m;
    public bool RequireCompletedEnrollment { get; set; } = false;
    public bool RequireAllPublishedAssessments { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}

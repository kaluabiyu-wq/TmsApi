using System.ComponentModel.DataAnnotations;

namespace TmsApi.Application.Certificates;

public sealed record IssueCertificateRequest(
    [Range(1, int.MaxValue)] int StudentId,
    [Range(1, int.MaxValue)] int CourseId,
    int? TemplateId);

public sealed record RevokeCertificateRequest(
    [Required, MinLength(5), MaxLength(500)] string Reason);

public sealed record CertificateDetailsDto(
    int Id,
    string SerialNumber,
    string Status,
    DateTime IssuedAt,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseName,
    string VerificationCode,
    string? RevocationReason);

public sealed record PublicCertificateVerificationDto(
    bool IsValid,
    string Status,
    string Message,
    string? SerialNumber,
    string? StudentName,
    string? CourseName,
    DateTime? IssuedAt,
    string? InstitutionName);

public sealed record CertificateEligibilityResult(
    bool IsEligible,
    IReadOnlyList<string> Reasons);


public sealed record CertificateTemplateDto(
    int Id,
    string Name,
    string InstitutionName,
    string? LogoPath,
    string? SignatureName,
    string? SignatureTitle,
    string CertificateTitle,
    string BodyText,
    decimal MinimumFinalGrade,
    decimal MinimumAttendancePercent,
    bool RequireCompletedEnrollment,
    bool RequireAllPublishedAssessments,
    bool IsActive);

public sealed record SaveCertificateTemplateRequest(
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(150)] string Name,
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(250)] string InstitutionName,
    string? LogoPath,
    string? SignatureName,
    string? SignatureTitle,
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(200)] string CertificateTitle,
    [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(1000)] string BodyText,
    [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0", "100")] decimal MinimumFinalGrade,
    [System.ComponentModel.DataAnnotations.Range(typeof(decimal), "0", "100")] decimal MinimumAttendancePercent,
    bool RequireCompletedEnrollment,
    bool RequireAllPublishedAssessments,
    bool IsActive);

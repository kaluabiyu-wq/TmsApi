using TmsApi.Domain.Entities;

namespace TmsApi.Application.Certificates;

public interface ICertificateEligibilityService
{
    Task<CertificateEligibilityResult> CheckAsync(
        int studentId, int courseId, CertificateTemplate template, CancellationToken ct);
}

public interface ICertificatePdfService
{
    Task<byte[]> GenerateAsync(
        Certificate certificate, CertificateTemplate template, string verificationUrl,
        CancellationToken ct);
}

public interface ICertificateFileStore
{
    Task<string> SaveAsync(string fileName, byte[] content, CancellationToken ct);
    Task<byte[]?> ReadAsync(string storageKey, CancellationToken ct);
}

public interface ICertificateIssuanceService
{
    Task<CertificateDetailsDto> IssueAsync(
        int studentId, int courseId, int? templateId, string issuerUserId,
        string? ipAddress, CancellationToken ct);
    Task<CertificateDetailsDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<CertificateDetailsDto>> GetAllAsync(int take, CancellationToken ct);
    Task<IReadOnlyList<CertificateDetailsDto>> GetForStudentAsync(
        int studentId, CancellationToken ct);
    Task<PublicCertificateVerificationDto> VerifyAsync(
        string code, string? ipAddress, CancellationToken ct);
    Task<(byte[] Content, string FileName)?> DownloadAsync(
        int certificateId, string userId, bool isStaff, string? ipAddress, CancellationToken ct);
    Task<bool> RevokeAsync(
        int certificateId, string reason, string issuerUserId, string? ipAddress, CancellationToken ct);
}


public interface ICertificateTemplateService
{
    Task<IReadOnlyList<CertificateTemplateDto>> GetAllAsync(CancellationToken ct);
    Task<CertificateTemplateDto> CreateAsync(SaveCertificateTemplateRequest request, CancellationToken ct);
    Task<CertificateTemplateDto?> UpdateAsync(int id, SaveCertificateTemplateRequest request, CancellationToken ct);
}

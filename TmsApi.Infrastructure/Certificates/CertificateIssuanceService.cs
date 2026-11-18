using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TmsApi.Application.Certificates;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Certificates;

public sealed class CertificateIssuanceService(
    TmsDbContext db,
    ICertificateEligibilityService eligibility,
    ICertificatePdfService pdf,
    ICertificateFileStore files,
    IConfiguration configuration) : ICertificateIssuanceService
{
    private string PublicBaseUrl
    {
        get
        {
            var value = configuration["Certificates:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException(
                    "Certificates:PublicBaseUrl must be configured before issuing certificates.");
            return value.TrimEnd('/');
        }
    }

    public async Task<CertificateDetailsDto> IssueAsync(
        int studentId, int courseId, int? templateId, string issuerUserId,
        string? ipAddress, CancellationToken ct)
    {
        var student = await db.Students.FirstOrDefaultAsync(x => x.ID == studentId, ct)
            ?? throw new KeyNotFoundException("Student was not found.");
        if (!student.IsActive)
            throw new InvalidOperationException("Certificates cannot be issued to an inactive student.");

        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == courseId, ct)
            ?? throw new KeyNotFoundException("Course was not found.");

        CertificateTemplate? template;
        if (templateId.HasValue)
        {
            template = await db.Set<CertificateTemplate>()
                .FirstOrDefaultAsync(x => x.Id == templateId.Value && x.IsActive, ct);
            if (template is null)
                throw new KeyNotFoundException("The selected certificate template was not found or is inactive.");
        }
        else
        {
            template = await db.Set<CertificateTemplate>()
                .Where(x => x.IsActive).OrderBy(x => x.Id).FirstOrDefaultAsync(ct);
            template ??= new CertificateTemplate();
        }

        var existing = await db.Certificates.FirstOrDefaultAsync(
            x => x.StudentId == studentId && x.CourseId == courseId, ct);
        if (existing is not null)
        {
            if (existing.Status == "Issued")
                throw new InvalidOperationException("An active certificate already exists for this student and course.");
            throw new InvalidOperationException("A certificate was previously issued and revoked. Contact an administrator before reissuing.");
        }

        var result = await eligibility.CheckAsync(studentId, courseId, template, ct);
        if (!result.IsEligible)
            throw new InvalidOperationException("Certificate eligibility failed: " + string.Join(" ", result.Reasons));

        var serial = $"TMS-{DateTime.UtcNow:yyyy}-{Guid.NewGuid():N}".ToUpperInvariant();
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var certificate = new Certificate
        {
            SerialNumber = serial,
            VerificationCode = code,
            Status = "Issued",
            IssuedAt = DateTime.UtcNow,
            IssuedBy = issuerUserId,
            StudentId = studentId,
            CourseId = courseId,
            TemplateId = template.Id == 0 ? null : template.Id,
            Student = student,
            Course = course,
            Template = template.Id == 0 ? null : template
        };

        var verifyUrl = $"{PublicBaseUrl}/certificate-verification/{code}";
        var bytes = await pdf.GenerateAsync(certificate, template, verifyUrl, ct);
        certificate.StorageKey = await files.SaveAsync($"{serial}.pdf", bytes, ct);
        certificate.CertificateUrl = verifyUrl;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Certificates.Add(certificate);
        await db.SaveChangesAsync(ct);
        db.Set<CertificateIssuanceLog>().Add(new CertificateIssuanceLog
        {
            CertificateId = certificate.Id,
            Action = "Issued",
            PerformedBy = issuerUserId,
            IpAddress = ipAddress,
            Details = $"Issued certificate {serial}."
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ToDto(certificate);
    }

    public async Task<CertificateDetailsDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var c = await db.Certificates.AsNoTracking()
            .Include(x => x.Student).Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return c is null ? null : ToDto(c);
    }

    public async Task<IReadOnlyList<CertificateDetailsDto>> GetAllAsync(
        int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 500);
        return await db.Certificates.AsNoTracking()
            .Include(x => x.Student).Include(x => x.Course)
            .OrderByDescending(x => x.IssuedAt)
            .Take(take)
            .Select(x => new CertificateDetailsDto(
                x.Id, x.SerialNumber, x.Status, x.IssuedAt, x.StudentId, x.Student.Name,
                x.CourseId, x.Course.Title, x.VerificationCode, x.RevocationReason))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CertificateDetailsDto>> GetForStudentAsync(
        int studentId, CancellationToken ct)
    {
        return await db.Certificates.AsNoTracking()
            .Include(x => x.Student).Include(x => x.Course)
            .Where(x => x.StudentId == studentId)
            .OrderByDescending(x => x.IssuedAt)
            .Select(x => new CertificateDetailsDto(
                x.Id, x.SerialNumber, x.Status, x.IssuedAt, x.StudentId, x.Student.Name,
                x.CourseId, x.Course.Title, x.VerificationCode, x.RevocationReason))
            .ToListAsync(ct);
    }

    public async Task<PublicCertificateVerificationDto> VerifyAsync(
        string code, string? ipAddress, CancellationToken ct)
    {
        var c = await db.Certificates.IgnoreQueryFilters().AsNoTracking()
            .Include(x => x.Student).Include(x => x.Course).Include(x => x.Template)
            .FirstOrDefaultAsync(x => x.VerificationCode == code, ct);

        if (c is null)
            return new(false, "NotFound", "No certificate matches this verification code.",
                null, null, null, null, null);

        db.Set<CertificateIssuanceLog>().Add(new CertificateIssuanceLog
        {
            CertificateId = c.Id,
            Action = "Verified",
            PerformedBy = "Anonymous",
            IpAddress = ipAddress,
            Details = "Public verification request."
        });
        await db.SaveChangesAsync(ct);

        var isValid = c.Status == "Issued" && !c.Student.IsDeleted;
        return new(
            isValid,
            isValid ? "Valid" : "Revoked",
            isValid ? "This certificate is authentic and currently valid."
                    : "This certificate is not currently valid.",
            c.SerialNumber,
            c.Student.Name,
            c.Course.Title,
            c.IssuedAt,
            c.Template?.InstitutionName ?? "Training Institution");
    }

    public async Task<(byte[] Content, string FileName)?> DownloadAsync(
        int certificateId, string userId, bool isStaff, string? ipAddress, CancellationToken ct)
    {
        var c = await db.Certificates.Include(x => x.Student)
            .FirstOrDefaultAsync(x => x.Id == certificateId, ct);
        if (c is null || (!isStaff && c.Student.UserId != userId))
            return null;

        if (c.Status != "Issued")
            throw new InvalidOperationException("Revoked certificates cannot be downloaded.");

        var content = await files.ReadAsync(c.StorageKey, ct);
        if (content is null) return null;

        db.Set<CertificateIssuanceLog>().Add(new CertificateIssuanceLog
        {
            CertificateId = c.Id,
            Action = "Downloaded",
            PerformedBy = userId,
            IpAddress = ipAddress
        });
        await db.SaveChangesAsync(ct);
        return (content, $"{c.SerialNumber}.pdf");
    }

    public async Task<bool> RevokeAsync(
        int certificateId, string reason, string issuerUserId, string? ipAddress, CancellationToken ct)
    {
        var c = await db.Certificates.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == certificateId, ct);
        if (c is null) return false;
        if (c.Status == "Revoked") return true;

        c.Status = "Revoked";
        c.RevokedAt = DateTime.UtcNow;
        c.RevocationReason = reason.Trim();

        db.Set<CertificateIssuanceLog>().Add(new CertificateIssuanceLog
        {
            CertificateId = c.Id,
            Action = "Revoked",
            PerformedBy = issuerUserId,
            IpAddress = ipAddress,
            Details = reason.Trim()
        });
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static CertificateDetailsDto ToDto(Certificate c) =>
        new(c.Id, c.SerialNumber, c.Status, c.IssuedAt, c.StudentId, c.Student.Name,
            c.CourseId, c.Course.Title, c.VerificationCode, c.RevocationReason);
}

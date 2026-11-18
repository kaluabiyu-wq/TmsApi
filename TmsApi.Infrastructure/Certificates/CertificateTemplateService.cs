using Microsoft.EntityFrameworkCore;
using TmsApi.Application.Certificates;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Certificates;

public sealed class CertificateTemplateService(TmsDbContext db) : ICertificateTemplateService
{
    public async Task<IReadOnlyList<CertificateTemplateDto>> GetAllAsync(CancellationToken ct)
    {
        var rows = await db.CertificateTemplates.AsNoTracking()
            .OrderBy(x => x.Name).ToListAsync(ct);
        return rows.Select(ToDto).ToList();
    }

    public async Task<CertificateTemplateDto> CreateAsync(
        SaveCertificateTemplateRequest request, CancellationToken ct)
    {
        Validate(request);
        var entity = new CertificateTemplate();
        Apply(entity, request);
        db.CertificateTemplates.Add(entity);
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    public async Task<CertificateTemplateDto?> UpdateAsync(
        int id, SaveCertificateTemplateRequest request, CancellationToken ct)
    {
        Validate(request);
        var entity = await db.CertificateTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return null;
        Apply(entity, request);
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToDto(entity);
    }

    private static void Validate(SaveCertificateTemplateRequest r)
    {
        if (r.MinimumFinalGrade is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(r.MinimumFinalGrade));
        if (r.MinimumAttendancePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(r.MinimumAttendancePercent));
    }

    private static void Apply(CertificateTemplate e, SaveCertificateTemplateRequest r)
    {
        e.Name = r.Name.Trim();
        e.InstitutionName = r.InstitutionName.Trim();
        e.LogoPath = string.IsNullOrWhiteSpace(r.LogoPath) ? null : r.LogoPath.Trim();
        e.SignatureName = r.SignatureName?.Trim();
        e.SignatureTitle = r.SignatureTitle?.Trim();
        e.CertificateTitle = r.CertificateTitle.Trim();
        e.BodyText = r.BodyText.Trim();
        e.MinimumFinalGrade = r.MinimumFinalGrade;
        e.MinimumAttendancePercent = r.MinimumAttendancePercent;
        e.RequireCompletedEnrollment = r.RequireCompletedEnrollment;
        e.RequireAllPublishedAssessments = r.RequireAllPublishedAssessments;
        e.IsActive = r.IsActive;
    }

    private static CertificateTemplateDto ToDto(CertificateTemplate x) =>
        new(x.Id, x.Name, x.InstitutionName, x.LogoPath, x.SignatureName, x.SignatureTitle,
            x.CertificateTitle, x.BodyText, x.MinimumFinalGrade, x.MinimumAttendancePercent,
            x.RequireCompletedEnrollment, x.RequireAllPublishedAssessments, x.IsActive);
}

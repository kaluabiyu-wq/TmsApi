using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class CertificateTemplateConfiguration : IEntityTypeConfiguration<CertificateTemplate>
{
    public void Configure(EntityTypeBuilder<CertificateTemplate> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.InstitutionName).HasMaxLength(250).IsRequired();
        b.Property(x => x.MinimumFinalGrade).HasPrecision(5, 2);
        b.Property(x => x.MinimumAttendancePercent).HasPrecision(5, 2);
        b.HasIndex(x => new { x.IsActive, x.Name });
    }
}

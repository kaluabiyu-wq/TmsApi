using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class CertificateIssuanceLogConfiguration : IEntityTypeConfiguration<CertificateIssuanceLog>
{
    public void Configure(EntityTypeBuilder<CertificateIssuanceLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Action).HasMaxLength(40).IsRequired();
        b.Property(x => x.PerformedBy).HasMaxLength(450).IsRequired();
        b.HasIndex(x => new { x.CertificateId, x.CreatedAt });
        b.HasOne(x => x.Certificate).WithMany(x => x.IssuanceLogs)
            .HasForeignKey(x => x.CertificateId).OnDelete(DeleteBehavior.Cascade);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public sealed class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> b)
    {
        b.ToTable("ReportDefinitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).HasMaxLength(80).IsRequired();
        b.HasIndex(x => x.Key).IsUnique();
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
    }
}

public sealed class GeneratedReportConfiguration : IEntityTypeConfiguration<GeneratedReport>
{
    public void Configure(EntityTypeBuilder<GeneratedReport> b)
    {
        b.ToTable("GeneratedReports");
        b.HasKey(x => x.Id);
        b.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Format).HasMaxLength(12).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.FiltersJson).HasColumnType("jsonb");
        b.Property(x => x.FileName).HasMaxLength(260);
        b.Property(x => x.StoragePath).HasMaxLength(1000);
        b.Property(x => x.ContentType).HasMaxLength(120);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.HasIndex(x => new { x.RequestedByUserId, x.RequestedAt });
        b.HasIndex(x => new { x.Status, x.RequestedAt });
        b.HasOne(x => x.ReportDefinition).WithMany().HasForeignKey(x => x.ReportDefinitionId).OnDelete(DeleteBehavior.Restrict);
    }
}

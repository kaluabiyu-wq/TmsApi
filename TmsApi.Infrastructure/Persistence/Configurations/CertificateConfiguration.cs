using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.SerialNumber).HasMaxLength(80).IsRequired();
        b.Property(x => x.VerificationCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.IssuedBy).HasMaxLength(450).IsRequired();
        b.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();

        b.HasIndex(x => x.SerialNumber).IsUnique();
        b.HasIndex(x => x.VerificationCode).IsUnique();
        b.HasIndex(x => new { x.StudentId, x.CourseId }).IsUnique();

        b.HasOne(x => x.Student).WithMany(x => x.Certificates)
            .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Course).WithMany(x => x.Certificates)
            .HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Template).WithMany(x => x.Certificates)
            .HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.SetNull);

        // Preserve the existing soft-delete behavior in the supplied project.
        b.HasQueryFilter(x => !x.Student.IsDeleted);
    }
}

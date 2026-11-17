using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class GradeAppealConfiguration : IEntityTypeConfiguration<GradeAppeal>
{
    public void Configure(EntityTypeBuilder<GradeAppeal> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Response).HasMaxLength(4000);
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.StudentId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.ReviewedBy).HasMaxLength(450);

        builder.HasOne(x => x.Enrollment)
            .WithMany()
            .HasForeignKey(x => x.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.EnrollmentId);
        builder.HasIndex(x => new { x.EnrollmentId, x.Status });
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class GradeChangeHistoryConfiguration : IEntityTypeConfiguration<GradeChangeHistory>
{
    public void Configure(EntityTypeBuilder<GradeChangeHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PreviousScore).HasPrecision(10, 2);
        builder.Property(x => x.NewScore).HasPrecision(10, 2);
        builder.Property(x => x.PreviousFinalGrade).HasPrecision(10, 2);
        builder.Property(x => x.NewFinalGrade).HasPrecision(10, 2);

        builder.Property(x => x.Reason).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.ChangedBy).HasMaxLength(450).IsRequired();

        builder.HasIndex(x => x.GradeRecordId);
        builder.HasIndex(x => x.EnrollmentId);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public sealed class GradeRecordConfiguration : IEntityTypeConfiguration<GradeRecord>
{
    public void Configure(EntityTypeBuilder<GradeRecord> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Score).HasPrecision(10, 2);
        b.Property(x => x.MaxScore).HasPrecision(10, 2);
        b.Property(x => x.Percentage).HasPrecision(7, 2);
        b.Property(x => x.Weight).HasPrecision(7, 2);
        b.HasIndex(x => new { x.StudentId, x.AssessmentId }).IsUnique();
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Assessment).WithMany().HasForeignKey(x => x.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Submission).WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GradeAppealConfiguration : IEntityTypeConfiguration<GradeAppeal>
{
    public void Configure(EntityTypeBuilder<GradeAppeal> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.ReviewComment).HasMaxLength(4000);
        b.HasIndex(x => new { x.GradeRecordId, x.Status });
        b.HasOne(x => x.GradeRecord).WithMany(x => x.Appeals).HasForeignKey(x => x.GradeRecordId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
    }
}

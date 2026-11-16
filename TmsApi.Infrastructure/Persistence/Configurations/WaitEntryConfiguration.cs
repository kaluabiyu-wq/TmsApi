using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class WaitlistEntryConfiguration : IEntityTypeConfiguration<WaitlistEntry>
{
    public void Configure(EntityTypeBuilder<WaitlistEntry> b)
    {
        b.HasKey(w => w.Id);

        b.Property(w => w.Status).IsRequired().HasMaxLength(20);
        b.Property(w => w.RequestedAt).HasColumnType("timestamp with time zone");
        b.Property(w => w.OfferedAt).HasColumnType("timestamp with time zone");
        b.Property(w => w.ExpiresAt).HasColumnType("timestamp with time zone");

        b.HasOne(w => w.Student).WithMany().HasForeignKey(w => w.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(w => w.Course).WithMany().HasForeignKey(w => w.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // A student may have only ONE live entry (Waiting/Offered) per course.
        // History rows (Accepted/Cancelled/Expired) are kept, so the index is partial.
        b.HasIndex(w => new { w.StudentId, w.CourseId })
            .IsUnique()
            .HasFilter("\"Status\" IN ('Waiting','Offered')")
            .HasDatabaseName("UX_WaitlistEntries_ActiveStudentCourse");

        // Queue lookups: "next Waiting entry for course X, oldest first".
        b.HasIndex(w => new { w.CourseId, w.Status, w.Position })
            .HasDatabaseName("IX_WaitlistEntries_Queue");

        // Same rule as Enrollment: hide entries of soft-deleted students.
        b.HasQueryFilter(w => !w.Student.IsDeleted);
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.HasKey(a => a.Id);

        // Enum <-> text. "Present", "Absent", "Late", "Excused" are what you see in the table.
        b.Property(a => a.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(a => a.Remarks).HasMaxLength(500);
        b.Property(a => a.RecordedBy).IsRequired().HasMaxLength(450);

        b.Property(a => a.CheckInAt).HasColumnType("timestamp with time zone");
        b.Property(a => a.RecordedAt).HasColumnType("timestamp with time zone");

        b.HasOne(a => a.Session).WithMany(s => s.AttendanceRecords).HasForeignKey(a => a.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // The database-level guarantee behind "one row per student per session".
        // AttendanceService upserts, so callers never hit this; concurrent requests might.
        b.HasIndex(a => new { a.SessionId, a.StudentId })
            .IsUnique()
            .HasDatabaseName("UX_AttendanceRecords_SessionStudent");

        // "Everything for student X" (student attendance report).
        b.HasIndex(a => a.StudentId)
            .HasDatabaseName("IX_AttendanceRecords_Student");

        // Same rule as Enrollment and WaitlistEntry: records of soft-deleted students are hidden
        // by default (use IgnoreQueryFilters() only for audit views).
        b.HasQueryFilter(a => !a.Student.IsDeleted);
    }
}

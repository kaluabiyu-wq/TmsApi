using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public sealed class CourseScheduleConfiguration : IEntityTypeConfiguration<CourseSchedule>
{
    public void Configure(EntityTypeBuilder<CourseSchedule> b)
    {
        b.ToTable("CourseSchedules");
        b.HasKey(x => x.Id);
        b.Property(x => x.InstructorId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Topic).HasMaxLength(300);
        b.Property(x => x.StartAt).HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.EndAt).HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone").IsRequired();

        b.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Instructor).WithMany().HasForeignKey(x => x.InstructorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.InstructorId, x.StartAt, x.EndAt });
        b.HasIndex(x => new { x.ClassroomId, x.StartAt, x.EndAt });
        b.HasIndex(x => new { x.CourseId, x.StartAt });
    }
}

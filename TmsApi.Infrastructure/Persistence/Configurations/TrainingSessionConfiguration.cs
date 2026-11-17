using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class TrainingSessionConfiguration : IEntityTypeConfiguration<TrainingSession>
{
    public void Configure(EntityTypeBuilder<TrainingSession> b)
    {
        b.HasKey(s => s.Id);

        b.Property(s => s.Topic).IsRequired().HasMaxLength(200);
        b.Property(s => s.InstructorId).IsRequired().HasMaxLength(450); // same length as AspNetUsers.Id

        b.Property(s => s.StartAt).HasColumnType("timestamp with time zone");
        b.Property(s => s.EndAt).HasColumnType("timestamp with time zone");
        b.Property(s => s.CreatedAt).HasColumnType("timestamp with time zone");

        // Sessions keep attendance history alive, so a course with sessions cannot be hard-deleted.
        b.HasOne(s => s.Course).WithMany().HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // "All sessions of course X in date order" is the hot query (session list, summary).
        b.HasIndex(s => new { s.CourseId, s.StartAt })
            .HasDatabaseName("IX_TrainingSessions_CourseStart");
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Title).IsRequired().HasMaxLength(200);
        b.HasMany(c => c.Enrollments).WithOne(e => e.Course)
        // b.HasMany(c => c.Enrollments)
        // .WithOne(e => e.Course)
        // .HasForeignKey(e=> e.CourseId)
        .OnDelete(DeleteBehavior.Restrict);
    }
}
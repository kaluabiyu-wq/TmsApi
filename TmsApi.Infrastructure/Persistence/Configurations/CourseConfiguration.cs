using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;


namespace TmsApi.Infrastructure.Persistence.Configurations;
public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.HasKey(c => c.Id);
        b.Property(c => c.Code).IsRequired().HasMaxLength(10);
        b.Property(c => c.Title).IsRequired().HasMaxLength(200);
        b.HasIndex(c => c.Code).IsUnique();
         b.HasMany(c => c.Enrollments).WithOne(e => e.Course)
         .HasForeignKey(e=> e.CourseId);
        b.HasOne(c => c.Instructor).WithMany().HasForeignKey(c => c.InstructorId)
        .IsRequired(false); 
       
    }
}
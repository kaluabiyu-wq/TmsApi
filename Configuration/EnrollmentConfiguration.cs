
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void  Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.HasKey(e => e.ID);
        b.HasIndex(e => new { e.StudentId, e.CourseId}).IsUnique();
        b.HasOne( e => e.Student).WithMany(s => s.Enrollments)
        .HasForeignKey(e => e.StudentId);
        b.HasOne(e => e.Course).WithMany(c => c.Enrollments)
        .HasForeignKey(e => e.CourseId);
    }


}
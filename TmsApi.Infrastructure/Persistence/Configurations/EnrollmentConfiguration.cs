
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;
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

        // Student is soft-deleted (Student.IsDeleted), and Enrollment requires a Student.
        // Without a matching filter, EF silently drops enrollments whenever a query joins
        // to a deleted student (and warns about it at startup). This makes the rule explicit:
        // enrollments of a deleted student are hidden by default. The rows stay in the
        // database, and an audit query can still read them with .IgnoreQueryFilters().
        b.HasQueryFilter(e => !e.Student.IsDeleted);
    }


}

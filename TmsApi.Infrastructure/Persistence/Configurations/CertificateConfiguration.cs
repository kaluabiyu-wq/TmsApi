using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> b)
    {
        // Same rule as Enrollment: Certificate requires a Student, and Student is soft-deleted.
        // Certificates of a deleted student are hidden by default but kept in the database;
        // use .IgnoreQueryFilters() for audit queries.
        b.HasQueryFilter(c => !c.Student.IsDeleted);
    }
}

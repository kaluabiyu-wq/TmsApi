using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> b)
    {
        b.HasKey(c => c.Id);

        b.Property(c => c.Name).IsRequired().HasMaxLength(100);
        b.Property(c => c.Location).IsRequired().HasMaxLength(200);

        // UTC instants are stored as timestamptz (see docs/API-STANDARDS.md).
        b.Property(c => c.CreatedAt).HasColumnType("timestamp with time zone");

        // The database is the last line of defence against duplicate names;
        // ClassroomService checks first so callers get a clean 409 instead of a 500.
        b.HasIndex(c => c.Name).IsUnique();
    }
}

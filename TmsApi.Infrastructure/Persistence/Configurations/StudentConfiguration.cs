

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;
public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasQueryFilter(s => !s.IsDeleted);
        builder.HasIndex(s => s.UserId).IsUnique().HasFilter("\"UserId\" IS NOT NULL");
        builder.Property<DateTime>("LastUpdated")
        .HasColumnType("timeStamp without time Zone")
        .HasDefaultValueSql("now()");
        builder.Property(s => s.Version).IsRowVersion();
        builder.Property(s => s.Status).HasMaxLength(30).HasDefaultValue("Active").IsRequired();
        
     
    

        
    }


}
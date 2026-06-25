

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Entities;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {

     builder.Property<DateTime>("LastUpdated")
     .HasColumnType("timeStamp without time Zone")
     .HasDefaultValueSql("now()");
    
    
    }


}
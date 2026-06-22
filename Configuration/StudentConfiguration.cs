
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
// using TmsApi.Entities;

// public class StudentConfiguration : IEntityTypeConfiguration<Student>
// {
//     public void Configure(EntityTypeBuilder<Student> builder)
//     {
//         builder.ToTable("Students");
//         builder.HasKey(s => s.ID);
//         builder.Property(s=>s.RegistrationNumber)
//         .IsRequired()
//         .HasMaxLength(20);
//         builder.HasIndex(s=> s.RegistrationNumber)
//         .IsUnique();
//         builder.Property(s=>s.Name)
//         .IsRequired()
//         .HasMaxLength(100);
//         builder.Property(s=>s.GPA)
//         .HasPrecision(4,2);
//         builder.Property(s=>s.IsActive)
//         .HasDefaultValue(true);

//     }
// }
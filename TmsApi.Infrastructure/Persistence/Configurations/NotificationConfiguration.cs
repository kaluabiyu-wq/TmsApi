using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TmsApi.Domain.Entities;

namespace TmsApi.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> b)
    {
        b.ToTable("Notifications");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Title).HasMaxLength(180).IsRequired();
        b.Property(x => x.Message).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Type).HasMaxLength(40).IsRequired();
        b.Property(x => x.Category).HasMaxLength(80).IsRequired();
        b.Property(x => x.ActionUrl).HasMaxLength(1000);
        b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.ReadAt).HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.UserId, x.CreatedAt });
        b.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        b.HasMany(x => x.Deliveries).WithOne(x => x.Notification)
            .HasForeignKey(x => x.NotificationId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> b)
    {
        b.ToTable("NotificationPreferences");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserId).HasMaxLength(450).IsRequired();
        b.Property(x => x.Category).HasMaxLength(80).IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.UserId, x.Category }).IsUnique();
    }
}

public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationDelivery> b)
    {
        b.ToTable("NotificationDeliveries");
        b.HasKey(x => x.Id);
        b.Property(x => x.Channel).HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasMaxLength(20).IsRequired();
        b.Property(x => x.LastError).HasMaxLength(2000);
        b.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.NextAttemptAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.SentAt).HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.Status, x.NextAttemptAt });
        b.HasIndex(x => new { x.NotificationId, x.Channel }).IsUnique();
    }
}

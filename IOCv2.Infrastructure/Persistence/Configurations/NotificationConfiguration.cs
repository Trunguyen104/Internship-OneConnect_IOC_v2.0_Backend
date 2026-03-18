using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.NotificationId);

        builder.Property(n => n.NotificationId)
            .HasColumnName("notification_id")
            .IsRequired();

        builder.Property(n => n.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(n => n.Title)
            .HasColumnName("title")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(n => n.Content)
            .HasColumnName("content")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.Type)
            .HasColumnName("type")
            .HasColumnType("smallint")
            .IsRequired();

        builder.Property(n => n.ReferenceType)
            .HasColumnName("reference_type")
            .HasMaxLength(100);

        builder.Property(n => n.ReferenceId)
            .HasColumnName("reference_id");

        builder.Property(n => n.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .HasColumnName("read_at");

        // FK → Users
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_notifications_users_user_id");

        // Index: tìm kiếm thông báo chưa đọc của user rất thường xuyên
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("ix_notifications_user_id_is_read");
    }
}

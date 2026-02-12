using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(u => u.UserId);

            builder.Property(u => u.UserCode).IsRequired().HasMaxLength(10);
            builder.HasIndex(u => u.UserCode).IsUnique();

            builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
            builder.HasIndex(u => u.Username).IsUnique();

            builder.Property(u => u.PasswordHash).IsRequired();

            builder.Property(u => u.Email).IsRequired().HasMaxLength(150);
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.FullName).IsRequired().HasMaxLength(100);

            builder.Property(u => u.PhoneNumber).IsRequired(false).HasMaxLength(15);
            builder.HasIndex(u => u.PhoneNumber).IsUnique();

            builder.Property(u => u.AvatarUrl).HasMaxLength(255);

            builder.Property(u => u.DateOfBirth);

            builder.Property(u => u.Gender)
                    .HasConversion<short>()
                    .IsRequired();

            builder.Property(u => u.Status)
                   .HasConversion<short>()
                   .HasDefaultValue(UserStatus.Active)
                   .HasSentinel(UserStatus.Inactive);

            builder.Property(u => u.Role)
                   .HasConversion<short>()
                   .IsRequired();

            builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
            builder.Property(u => u.CreatedAt).HasColumnName("created_at");
            builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            builder.Property(u => u.CreatedBy).HasColumnName("created_by");
            builder.Property(u => u.UpdatedBy).HasColumnName("updated_by");

            // Global Query Filter: Hide soft-deleted employees
            builder.HasQueryFilter(u => u.DeletedAt == null);


            builder.HasIndex(u => u.Role);
            builder.HasIndex(u => u.Status);
            builder.HasIndex(u => u.CreatedAt);

            // Composite indexes for common queries
            builder.HasIndex(u => new { u.Status, u.Role });
            builder.HasIndex(u => new { u.Role, u.Status });

            // Filtered index for active employees
            builder.HasIndex(u => u.Status)
                .HasFilter("deleted_at IS NULL");


        }
    }
}
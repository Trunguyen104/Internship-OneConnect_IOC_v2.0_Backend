using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Username)
                   .IsRequired()
                   .HasMaxLength(50);
            builder.HasIndex(u => u.Username)
                   .IsUnique();

            builder.Property(u => u.PasswordHash)
                   .IsRequired();

            builder.Property(u => u.Email)
                   .IsRequired()
                   .HasMaxLength(150);
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.FullName)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(u => u.PhoneNumber)
                    .IsRequired(false)
                   .HasMaxLength(15);
            builder.HasIndex(u => u.PhoneNumber)
                   .IsUnique();

            builder.Property(u => u.AvatarUrl)
                   .HasMaxLength(255);

            builder.Property(u => u.DateOfBirth);

            builder.Property(x => x.Status)
                   .HasConversion<short>()
                   .HasDefaultValue(UserStatus.Active)
                   .HasSentinel(UserStatus.Inactive);

            builder.Property(x => x.Role)
                   .HasConversion<short>()
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .HasDefaultValueSql("now()");

            builder.Property(x => x.UpdatedAt)
                   .HasDefaultValueSql("now()");

            builder.HasIndex(x => x.Role);
            builder.HasIndex(x => x.Status);
        }
    }
}
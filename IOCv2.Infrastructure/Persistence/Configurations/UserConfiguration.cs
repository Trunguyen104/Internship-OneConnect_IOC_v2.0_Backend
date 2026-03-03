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

            builder.HasIndex(u => u.CreatedAt);

            // Tối ưu hoá Indexed: Composite indexes bắt đầu bằng Role sẽ cover cho HasIndex(u => u.Role) ở trên.
            // Chỉ giữ lại một cặp Left-most Prefix tốt nhất hỗ trợ tìm kiếm thường dùng
            builder.HasIndex(u => new { u.Role, u.Status });

            // Filtered index cho UserStatus để optimize việc tìm kiếm (deleted_at IS NULL)
            builder.HasIndex(u => u.Status)
                .HasFilter("deleted_at IS NULL");


            // Relationships
            builder.HasOne(u => u.Student)
                .WithOne(s => s.User)
                .HasForeignKey<Student>(s => s.UserId);

            builder.HasOne(u => u.UniversityUser)
                .WithOne(uu => uu.User)
                .HasForeignKey<UniversityUser>(uu => uu.UserId);

            builder.HasOne(u => u.EnterpriseUser)
                .WithOne(eu => eu.User)
                .HasForeignKey<EnterpriseUser>(eu => eu.UserId);
        }
    }
}
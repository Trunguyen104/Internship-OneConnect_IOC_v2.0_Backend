using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
    { 
        public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
        {
            builder.ToTable("password_reset_tokens");
            builder.HasKey(e => e.TokenId)
                   .HasName("pk_password_reset_tokens");
            builder.Property(e => e.TokenId)
                   .HasColumnName("token_id")
                   .IsRequired();
            builder.Property(e => e.EmployeeId)
                   .HasColumnName("employee_id")
                   .IsRequired();
            builder.Property(e => e.TokenHash)
                   .HasColumnName("token_hash")
                   .HasMaxLength(64)
                   .IsRequired();
            builder.Property(e => e.ExpiresAt)
                   .HasColumnName("expires_at")
                   .IsRequired();
            builder.Property(e => e.UsedAt)
                   .HasColumnName("used_at");
            builder.Property(e => e.CreatedAt)
                   .HasColumnName("created_at")
                   .HasDefaultValueSql("now()")
                   .IsRequired();
            builder.HasOne(e => e.User)
                   .WithMany()
                   .HasForeignKey(e => e.EmployeeId)
                   .HasConstraintName("fk_password_reset_tokens_users_employee_id")
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

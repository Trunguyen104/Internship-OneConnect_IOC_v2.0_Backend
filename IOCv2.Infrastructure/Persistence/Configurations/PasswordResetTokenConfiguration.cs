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

            builder.HasKey(e => e.Id)
                   .HasName("pk_password_reset_tokens");

            builder.Property(e => e.Id)
                   .HasColumnName("token_id")
                   .IsRequired();
            builder.Property(e => e.UserId)
                   .HasColumnName("user_id")
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
                   .HasForeignKey(e => e.UserId)
                   .HasConstraintName("fk_password_reset_tokens_users_user_id")
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
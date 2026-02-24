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
            builder.HasKey(t => t.TokenId);
            builder.Property(t => t.TokenId).HasColumnName("token_id");

            builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(64); // SHA256 hex string is 64 chars
            builder.HasIndex(t => t.TokenHash).IsUnique();

            builder.Property(t => t.ExpiresAt).IsRequired();
            builder.HasIndex(t => t.ExpiresAt);

            builder.Property(t => t.UsedAt);

            builder.Property(t => t.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");

            // Foreign key relationship
            builder.HasOne(t => t.User)
                   .WithMany()
                   .HasForeignKey(t => t.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(t => t.UserId);
        }
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");
            builder.HasKey(rt => rt.RefreshTokenId);
            builder.Property(rt => rt.RefreshTokenId).HasColumnName("id");
            // Token là chuỗi Unique và rất quan trọng để tìm kiếm
            builder.Property(rt => rt.Token)
                   .IsRequired()
                   .HasMaxLength(200); // Base64 32 bytes ~ 44 chars, nhưng cứ cho dư giả

            builder.HasIndex(rt => rt.Token).IsUnique();

            builder.Property(rt => rt.Expires).IsRequired();

            builder.Property(rt => rt.IsRevoked).HasDefaultValue(false);

            builder.Property(rt => rt.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            builder.Property(rt => rt.UpdatedAt).HasColumnName("updated_at");

            // Quan hệ với User
            builder.HasOne(rt => rt.User)
                   .WithMany(rt => rt.RefreshTokens)
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rt => rt.UserId);
        }
    }
}
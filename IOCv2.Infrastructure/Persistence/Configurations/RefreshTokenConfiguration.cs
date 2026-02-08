using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);
            // Token là chuỗi Unique và rất quan trọng để tìm kiếm
            builder.Property(x => x.Token)
                   .IsRequired()
                   .HasMaxLength(200); // Base64 32 bytes ~ 44 chars, nhưng cứ cho dư giả

            builder.HasIndex(x => x.Token)
                   .IsUnique();

            builder.Property(rt => rt.Expires)
                   .IsRequired();

            builder.Property(rt => rt.IsRevoked)
                   .HasDefaultValue(false);

            builder.Property(x => x.CreatedAt)
                  .HasDefaultValueSql("now()");

            // Quan hệ với User
            builder.HasOne(rt => rt.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rt => rt.UserId);
        }
    }
}
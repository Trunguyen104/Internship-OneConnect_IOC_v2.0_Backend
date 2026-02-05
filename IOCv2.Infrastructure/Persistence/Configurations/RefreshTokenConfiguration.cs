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
            builder.Property(rt => rt.Token)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(rt => rt.Token)
                   .IsRequired();

            builder.Property(rt => rt.Expires)
                   .IsRequired();

            builder.Property(rt => rt.IsRevoked)
                   .HasDefaultValue(false);

            builder.Property(rt => rt.CreatedAt)
                   .HasDefaultValue("now()");

            builder.HasOne(rt => rt.User)
                   .WithMany(u => u.RefreshTokens)
                   .HasForeignKey(rt => rt.EmployeeId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(rt => rt.User.Id);
        }
    }
}

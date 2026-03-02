using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class EnterpriseUserConfiguration : IEntityTypeConfiguration<EnterpriseUser>
    {
        public void Configure(EntityTypeBuilder<EnterpriseUser> builder)
        {
            builder.ToTable("enterprise_users");
            builder.HasKey(eu => eu.EnterpriseUserId);
            builder.Property(eu => eu.EnterpriseUserId).HasColumnName("enterprise_user_id");

            builder.Property(eu => eu.EnterpriseId).HasColumnName("enterprise_id").IsRequired();
            builder.Property(eu => eu.UserId).HasColumnName("user_id").IsRequired();
            builder.Property(eu => eu.Position).HasMaxLength(100).HasColumnName("position");

            builder.Property(eu => eu.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            builder.Property(eu => eu.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            builder.Property(eu => eu.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
            builder.Property(eu => eu.CreatedBy).HasColumnName("created_by");
            builder.Property(eu => eu.UpdatedBy).HasColumnName("updated_by");

            builder.HasIndex(eu => eu.EnterpriseId);
            builder.HasIndex(eu => eu.UserId).IsUnique();

            builder.HasOne(eu => eu.Enterprise)
                .WithMany(e => e.EnterpriseUsers)
                .HasForeignKey(eu => eu.EnterpriseId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(eu => eu.User)
                .WithOne(u => u.EnterpriseUser)
                .HasForeignKey<EnterpriseUser>(eu => eu.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        }
    }
}

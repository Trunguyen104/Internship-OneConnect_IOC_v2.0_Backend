using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class EnterpriseConfiguration : IEntityTypeConfiguration<Enterprise>
    {
        public void Configure(EntityTypeBuilder<Enterprise> builder)
        {
            builder.ToTable("enterprises");
            builder.HasKey(e => e.EnterpriseId);
            builder.Property(e => e.EnterpriseId).HasColumnName("enterprise_id");

            builder.Property(e => e.TaxCode).HasMaxLength(50).HasColumnName("tax_code");
            builder.HasIndex(e => e.TaxCode).IsUnique();

            builder.Property(e => e.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.HasIndex(e => e.Name);

            builder.Property(e => e.Industry).HasMaxLength(150).HasColumnName("industry");
            builder.Property(e => e.Description).HasColumnName("description");
            builder.Property(e => e.Address).HasMaxLength(500).HasColumnName("address");
            builder.Property(e => e.Website).HasMaxLength(255).HasColumnName("website");
            builder.Property(e => e.LogoUrl).HasMaxLength(255).HasColumnName("logo_url");
            builder.Property(e => e.BackgroundUrl).HasMaxLength(255).HasColumnName("background_url");
            
            builder.Property(e => e.Status).HasDefaultValue((short)EnterpriseStatus.Active).HasColumnName("status");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
        }
    }
}

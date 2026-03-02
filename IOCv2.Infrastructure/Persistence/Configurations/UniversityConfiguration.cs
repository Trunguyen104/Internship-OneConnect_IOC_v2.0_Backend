using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class UniversityConfiguration : IEntityTypeConfiguration<University>
    {
        public void Configure(EntityTypeBuilder<University> builder)
        {
            builder.ToTable("universities");
            builder.HasKey(u => u.UniversityId);
            builder.Property(u => u.UniversityId).HasColumnName("uni_id");

            builder.Property(u => u.Code).IsRequired().HasMaxLength(20).HasColumnName("code");
            builder.HasIndex(u => u.Code).IsUnique();

            builder.Property(u => u.Name).IsRequired().HasMaxLength(255).HasColumnName("name");
            builder.Property(u => u.Address).HasMaxLength(500).HasColumnName("address");
            builder.Property(u => u.LogoUrl).HasMaxLength(255).HasColumnName("logo_url");

            builder.Property(u => u.Status).HasDefaultValue(1).HasColumnName("status");

            builder.Property(u => u.CreatedAt).HasColumnName("created_at");
            builder.Property(u => u.CreatedBy).HasColumnName("created_by");
            builder.Property(u => u.UpdatedAt).HasColumnName("updated_at");
            builder.Property(u => u.UpdatedBy).HasColumnName("updated_by");
            builder.Property(u => u.DeletedAt).HasColumnName("deleted_at");
        }
    }
}

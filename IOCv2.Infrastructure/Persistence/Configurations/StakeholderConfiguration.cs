using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class StakeholderConfiguration : IEntityTypeConfiguration<Stakeholder>
    {
        public void Configure(EntityTypeBuilder<Stakeholder> builder)
        {
            builder.ToTable("stakeholders");
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(s => s.InternshipId)
                .HasColumnName("internship_id")
                .IsRequired();

            builder.Property(s => s.Name)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("name");

            builder.Property(s => s.Type)
                .HasConversion<short>()
                .HasDefaultValue(StakeholderType.Real)
                .HasColumnName("type");

            builder.Property(s => s.Role)
                .HasMaxLength(100)
                .HasColumnName("role");

            builder.Property(s => s.Description)
                .HasMaxLength(500)
                .HasColumnName("description");

            builder.Property(s => s.Email)
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnName("email");

            builder.Property(s => s.PhoneNumber)
                .HasMaxLength(15)
                .HasColumnName("phone_number");

            builder.Property(s => s.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()");

            builder.Property(s => s.CreatedBy)
                .HasColumnName("created_by");

            builder.Property(s => s.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(s => s.UpdatedBy)
                .HasColumnName("updated_by");

            builder.Property(s => s.DeletedAt)
                .HasColumnName("deleted_at");

            builder.HasIndex(s => s.InternshipId)
                .HasDatabaseName("ix_stakeholders_internship_id");

            builder.HasIndex(s => s.Email)
                .HasDatabaseName("ix_stakeholders_email");

            builder.HasIndex(s => new { s.InternshipId, s.Email })
                .IsUnique()
                .HasFilter("deleted_at IS NULL")
                .HasDatabaseName("ix_stakeholders_internship_email_unique");

            builder.HasOne(s => s.InternshipGroup)
                .WithMany(p => p.Stakeholders)
                .HasForeignKey(s => s.InternshipId)
                .HasConstraintName("fk_stakeholders_internship_groups")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

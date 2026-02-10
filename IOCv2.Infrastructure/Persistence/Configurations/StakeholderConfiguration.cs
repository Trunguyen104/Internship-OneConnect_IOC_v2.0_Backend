using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class StakeholderConfiguration : IEntityTypeConfiguration<Stakeholder>
{
    public void Configure(EntityTypeBuilder<Stakeholder> builder)
    {
        builder.ToTable("stakeholders");
        
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.ProjectId)
            .IsRequired();
        
        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(s => s.Type)
            .HasConversion<short>()
            .IsRequired()
            .HasDefaultValue(StakeholderType.Real);
        
        builder.Property(s => s.Role)
            .HasMaxLength(100);
        
        builder.Property(s => s.Description)
            .HasMaxLength(500);
        
        builder.Property(s => s.Email)
            .IsRequired()
            .HasMaxLength(150);
        
        builder.Property(s => s.PhoneNumber)
            .HasMaxLength(15);
        
        builder.Property(s => s.CreatedAt)
            .IsRequired();
        
        builder.Property(s => s.CreatedBy)
            .HasMaxLength(100);
        
        builder.Property(s => s.UpdatedAt);
        
        builder.Property(s => s.UpdatedBy)
            .HasMaxLength(100);
        
        builder.Property(s => s.DeletedAt);
        
        // Indexes
        builder.HasIndex(s => s.ProjectId)
            .HasDatabaseName("ix_stakeholders_project_id");
        
        builder.HasIndex(s => s.Email)
            .HasDatabaseName("ix_stakeholders_email");
        
        builder.HasIndex(s => new { s.ProjectId, s.Email })
            .IsUnique()
            .HasDatabaseName("ix_stakeholders_project_email_unique")
            .HasFilter("deleted_at IS NULL");
        
        // Foreign key constraint
        builder.HasOne(s => s.Project)
            .WithMany(p => p.Stakeholders)
            .HasForeignKey(s => s.ProjectId)
            .HasConstraintName("fk_stakeholders_projects")
            .OnDelete(DeleteBehavior.Restrict);
    }
}


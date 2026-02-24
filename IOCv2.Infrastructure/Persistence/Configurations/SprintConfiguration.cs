using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class SprintConfiguration : IEntityTypeConfiguration<Sprint>
{
    public void Configure(EntityTypeBuilder<Sprint> builder)
    {
        builder.ToTable("sprints");
        
        builder.HasKey(s => s.SprintId);
        
        builder.Property(s => s.SprintId)
            .HasColumnName("sprint_id")
            .IsRequired();
        
        builder.Property(s => s.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();
        
        builder.Property(s => s.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(s => s.Goal)
            .HasColumnName("goal")
            .HasMaxLength(1000);
        
        builder.Property(s => s.StartDate)
            .HasColumnName("start_date");
        
        builder.Property(s => s.EndDate)
            .HasColumnName("end_date");
        
        builder.Property(s => s.Status)
            .HasColumnName("status")
            .IsRequired();
        
        // Base entity properties
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        
        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(s => s.DeletedAt)
            .HasColumnName("deleted_at");
        
        builder.Property(s => s.CreatedBy)
            .HasColumnName("created_by");
        
        builder.Property(s => s.UpdatedBy)
            .HasColumnName("updated_by");
        
        // Indexes
        builder.HasIndex(s => s.ProjectId)
            .HasDatabaseName("ix_sprints_project_id");
        
        builder.HasIndex(s => s.Status)
            .HasDatabaseName("ix_sprints_status");
    }
}

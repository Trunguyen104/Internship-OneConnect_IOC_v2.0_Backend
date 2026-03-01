using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(s => s.Goal)
            .HasColumnName("goal")
            .HasMaxLength(500);

        builder.Property(s => s.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("date");

        builder.Property(s => s.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("date");

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();

        // Base entity properties
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
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

        // Soft delete filter
        builder.HasQueryFilter(s => s.DeletedAt == null);
    }
}

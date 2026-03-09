using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class EvaluationCriteriaConfiguration : IEntityTypeConfiguration<EvaluationCriteria>
{
    public void Configure(EntityTypeBuilder<EvaluationCriteria> builder)
    {
        builder.ToTable("evaluation_criteria");

        builder.HasKey(e => e.CriteriaId);

        builder.Property(e => e.CriteriaId)
            .HasColumnName("criteria_id")
            .IsRequired();

        builder.Property(e => e.CycleId)
            .HasColumnName("cycle_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(e => e.MaxScore)
            .HasColumnName("max_score")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(e => e.Weight)
            .HasColumnName("weight")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        // Base entity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at");

        // Indexes
        builder.HasIndex(e => e.CycleId)
            .HasDatabaseName("ix_evaluation_criteria_cycle_id");

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}

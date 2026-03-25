using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class EvaluationCycleConfiguration : IEntityTypeConfiguration<EvaluationCycle>
{
    public void Configure(EntityTypeBuilder<EvaluationCycle> builder)
    {
        builder.ToTable("evaluation_cycles");

        builder.HasKey(e => e.CycleId);

        builder.Property(e => e.CycleId)
            .HasColumnName("cycle_id")
            .IsRequired();

        builder.Property(e => e.PhaseId)
            .HasColumnName("phase_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.StartDate)
            .HasColumnName("start_date")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.EndDate)
            .HasColumnName("end_date")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<short>()
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

        // Relationships
        builder.HasMany(e => e.Criteria)
            .WithOne(c => c.Cycle)
            .HasForeignKey(c => c.CycleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.PhaseId)
            .HasDatabaseName("ix_evaluation_cycles_phase_id");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("ix_evaluation_cycles_status");

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}

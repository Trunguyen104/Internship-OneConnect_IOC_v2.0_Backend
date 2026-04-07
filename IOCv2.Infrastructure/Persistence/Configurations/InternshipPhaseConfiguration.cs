using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class InternshipPhaseConfiguration : IEntityTypeConfiguration<InternshipPhase>
{
    public void Configure(EntityTypeBuilder<InternshipPhase> builder)
    {
        builder.ToTable("internship_phases");
        builder.HasKey(e => e.PhaseId);

        builder.Property(e => e.PhaseId).HasColumnName("phase_id").IsRequired();
        builder.Property(e => e.EnterpriseId).HasColumnName("enterprise_id").IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
        builder.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("date").IsRequired();
        builder.Property(e => e.EndDate).HasColumnName("end_date").HasColumnType("date").IsRequired();
        builder.Property(e => e.MajorFields).HasColumnName("major_fields").HasColumnType("text").IsRequired();
        builder.Property(e => e.Capacity).HasColumnName("capacity").IsRequired();
        builder.Property(e => e.Description).HasColumnName("description");
        builder.Property(e => e.Status).HasColumnName("status").HasConversion<short>().IsRequired();

        // Audit columns
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(e => e.Enterprise)
            .WithMany()
            .HasForeignKey(e => e.EnterpriseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(e => e.InternshipGroups)
            .WithOne(g => g.InternshipPhase)
            .HasForeignKey(g => g.PhaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.EvaluationCycles)
            .WithOne(c => c.InternshipPhase)
            .HasForeignKey(c => c.PhaseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.EnterpriseId).HasDatabaseName("ix_internship_phases_enterprise_id");
        builder.HasIndex(e => new { e.EnterpriseId, e.Status }).HasDatabaseName("ix_internship_phases_enterprise_status");
        builder.HasIndex(e => new { e.StartDate, e.EndDate }).HasDatabaseName("ix_internship_phases_dates");
        builder.HasIndex(e => new { e.EnterpriseId, e.Name })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_internship_phases_enterprise_name");
    }
}
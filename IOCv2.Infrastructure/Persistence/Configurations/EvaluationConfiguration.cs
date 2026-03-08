using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> builder)
    {
        builder.ToTable("evaluations");

        builder.HasKey(e => e.EvaluationId);

        builder.Property(e => e.EvaluationId)
            .HasColumnName("evaluation_id")
            .IsRequired();

        builder.Property(e => e.CycleId)
            .HasColumnName("cycle_id")
            .IsRequired();

        builder.Property(e => e.InternshipId)
            .HasColumnName("internship_id")
            .IsRequired();

        builder.Property(e => e.StudentId)
            .HasColumnName("student_id")
            .IsRequired(false);

        builder.Property(e => e.EvaluatorId)
            .HasColumnName("evaluator_id")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(e => e.TotalScore)
            .HasColumnName("total_score")
            .HasColumnType("numeric(5,2)");

        builder.Property(e => e.Note)
            .HasColumnName("note")
            .HasMaxLength(2000);

        // Base entity columns
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(e => e.CreatedBy).HasColumnName("created_by");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(e => e.Cycle)
            .WithMany()
            .HasForeignKey(e => e.CycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Internship)
            .WithMany()
            .HasForeignKey(e => e.InternshipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Evaluator)
            .WithMany()
            .HasForeignKey(e => e.EvaluatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Details)
            .WithOne(d => d.Evaluation)
            .HasForeignKey(d => d.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique per individual: 1 student chỉ có 1 evaluation trong 1 cycle
        // Unique per group: 1 nhóm chỉ có 1 group evaluation trong 1 cycle
        builder.HasIndex(e => new { e.CycleId, e.InternshipId, e.StudentId })
            .IsUnique()
            .HasDatabaseName("ix_evaluations_cycle_internship_student_unique");

        builder.HasIndex(e => e.InternshipId)
            .HasDatabaseName("ix_evaluations_internship_id");

        builder.HasQueryFilter(e => e.DeletedAt == null);
    }
}

using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class EvaluationDetailConfiguration : IEntityTypeConfiguration<EvaluationDetail>
{
    public void Configure(EntityTypeBuilder<EvaluationDetail> builder)
    {
        builder.ToTable("evaluation_details");

        builder.HasKey(d => d.DetailId);

        builder.Property(d => d.DetailId)
            .HasColumnName("detail_id")
            .IsRequired();

        builder.Property(d => d.EvaluationId)
            .HasColumnName("evaluation_id")
            .IsRequired();

        builder.Property(d => d.CriteriaId)
            .HasColumnName("criteria_id")
            .IsRequired();

        builder.Property(d => d.Score)
            .HasColumnName("score")
            .HasColumnType("numeric(5,2)")
            .IsRequired();

        builder.Property(d => d.Comment)
            .HasColumnName("comment")
            .HasMaxLength(1000);

        // Base entity columns
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(d => d.CreatedBy).HasColumnName("created_by");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at");
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by");
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(d => d.Evaluation)
            .WithMany(e => e.Details)
            .HasForeignKey(d => d.EvaluationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Criteria)
            .WithMany()
            .HasForeignKey(d => d.CriteriaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique: mỗi criteria chỉ được chấm 1 lần trong 1 evaluation
        builder.HasIndex(d => new { d.EvaluationId, d.CriteriaId })
            .IsUnique()
            .HasDatabaseName("ix_evaluation_details_evaluation_criteria_unique");

        builder.HasQueryFilter(d => d.DeletedAt == null);
    }
}

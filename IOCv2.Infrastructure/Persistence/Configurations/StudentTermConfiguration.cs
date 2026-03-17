using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class StudentTermConfiguration : IEntityTypeConfiguration<StudentTerm>
{
    public void Configure(EntityTypeBuilder<StudentTerm> builder)
    {
        builder.ToTable("student_terms");

        // Primary key
        builder.HasKey(x => x.StudentTermId);
        builder.Property(x => x.StudentTermId)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // Foreign keys
        builder.Property(x => x.TermId).HasColumnName("term_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.EnterpriseId).HasColumnName("enterprise_id");

        // Status columns
        builder.Property(x => x.EnrollmentStatus)
            .HasColumnName("enrollment_status")
            .HasColumnType("smallint")
            .HasDefaultValue(EnrollmentStatus.Active)
            .IsRequired();

        builder.Property(x => x.PlacementStatus)
            .HasColumnName("placement_status")
            .HasColumnType("smallint")
            .HasDefaultValue(PlacementStatus.Unplaced)
            .IsRequired();

        // Detail columns
        builder.Property(x => x.EnrollmentDate)
            .HasColumnName("enrollment_date")
            .HasColumnType("date")
            .HasDefaultValueSql("CURRENT_DATE")
            .IsRequired();

        builder.Property(x => x.EnrollmentNote)
            .HasColumnName("enrollment_note")
            .HasColumnType("text");

        builder.Property(x => x.MidtermFeedback)
            .HasColumnName("midterm_feedback")
            .HasColumnType("text");

        builder.Property(x => x.FinalFeedback)
            .HasColumnName("final_feedback")
            .HasColumnType("text");

        // Audit columns (from BaseEntity)
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");

        // Relationships
        builder.HasOne(x => x.Student)
            .WithMany(s => s.StudentTerms)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Term)
            .WithMany(t => t.StudentTerms)
            .HasForeignKey(x => x.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Enterprise)
            .WithMany(e => e.StudentTerms)
            .HasForeignKey(x => x.EnterpriseId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(x => new { x.StudentId, x.TermId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_student_term");

        builder.HasIndex(x => x.TermId)
            .HasDatabaseName("idx_student_terms_term_id");

        builder.HasIndex(x => new { x.TermId, x.PlacementStatus, x.EnrollmentStatus })
            .HasDatabaseName("idx_student_terms_statuses");
    }
}

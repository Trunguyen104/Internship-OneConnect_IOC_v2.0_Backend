using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class StudentTermConfiguration : IEntityTypeConfiguration<StudentTerm>
{
    public void Configure(EntityTypeBuilder<StudentTerm> builder)
    {
        builder.ToTable("student_terms");

        builder.HasKey(x => x.StudentTermId);

        builder.Property(x => x.StudentTermId).HasColumnName("student_term_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.TermId).HasColumnName("term_id");
        builder.Property(x => x.EnterpriseId).HasColumnName("enterprise_id");
        builder.Property(x => x.EnrollmentStatus).HasColumnName("enrollment_status").HasColumnType("smallint");
        builder.Property(x => x.PlacementStatus).HasColumnName("placement_status").HasColumnType("smallint");
        builder.Property(x => x.EnrollmentDate).HasColumnName("enrollment_date");
        builder.Property(x => x.EnrollmentNote).HasColumnName("enrollment_note").HasMaxLength(500);
        builder.Property(x => x.MidtermFeedback).HasColumnName("midterm_feedback").HasMaxLength(2000);
        builder.Property(x => x.FinalFeedback).HasColumnName("final_feedback").HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Unique: one active enrollment per (StudentId, TermId)
        builder.HasIndex(x => new { x.StudentId, x.TermId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL");

        // Composite index for common list queries
        builder.HasIndex(x => new { x.TermId, x.PlacementStatus, x.EnrollmentStatus });

        builder.HasOne(x => x.Student)
            .WithMany(s => s.StudentTerms)
            .HasForeignKey(x => x.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Term)
            .WithMany(t => t.StudentTerms)
            .HasForeignKey(x => x.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Enterprise)
            .WithMany()
            .HasForeignKey(x => x.EnterpriseId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

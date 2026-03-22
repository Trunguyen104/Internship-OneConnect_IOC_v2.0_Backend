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

        builder.HasKey(x => x.StudentTermId);

        builder.Property(x => x.StudentTermId)
            .HasColumnName("student_term_id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.TermId).HasColumnName("term_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.EnterpriseId).HasColumnName("enterprise_id");

        builder.Property(x => x.EnrollmentStatus)
            .HasColumnName("enrollment_status")
            .HasColumnType("smallint")
            .HasDefaultValue(EnrollmentStatus.Active);

        builder.Property(x => x.PlacementStatus)
            .HasColumnName("placement_status")
            .HasColumnType("smallint")
            .HasDefaultValue(PlacementStatus.Unplaced);

        builder.Property(x => x.EnrollmentDate)
            .HasColumnName("enrollment_date")
            .HasDefaultValueSql("CURRENT_DATE");

        // text columns — PostgreSQL text (không giới hạn độ dài)
        builder.Property(x => x.EnrollmentNote)
            .HasColumnName("enrollment_note")
            .HasColumnType("text");

        builder.Property(x => x.MidtermFeedback)
            .HasColumnName("midterm_feedback")
            .HasColumnType("text");

        builder.Property(x => x.FinalFeedback)
            .HasColumnName("final_feedback")
            .HasColumnType("text");

        // Audit
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        // Soft delete
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
        builder.Property(x => x.DeletedBy).HasColumnName("deleted_by");

        // Partial unique index: 1 SV chỉ có 1 record active/withdrawn trong 1 kỳ (nếu chưa bị xóa)
        builder.HasIndex(x => new { x.StudentId, x.TermId })
            .IsUnique()
            .HasDatabaseName("uq_student_term")
            .HasFilter("deleted_at IS NULL");

        // Tối ưu query get list theo kỳ
        builder.HasIndex(x => x.TermId)
            .HasDatabaseName("idx_student_terms_term_id");

        // Tối ưu đếm + filter theo trạng thái
        builder.HasIndex(x => new { x.TermId, x.PlacementStatus, x.EnrollmentStatus })
            .HasDatabaseName("idx_student_terms_statuses");

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

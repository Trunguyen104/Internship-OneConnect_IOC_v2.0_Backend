using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class InternshipApplicationConfiguration : IEntityTypeConfiguration<InternshipApplication>
{
    public void Configure(EntityTypeBuilder<InternshipApplication> builder)
    {
        builder.ToTable("internship_applications");

        builder.HasKey(x => x.ApplicationId);

        builder.Property(x => x.ApplicationId).HasColumnName("application_id");
        builder.Property(x => x.EnterpriseId).HasColumnName("enterprise_id").IsRequired();
        builder.Property(x => x.TermId).HasColumnName("term_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.JobId).HasColumnName("job_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.Property(x => x.Source)
            .HasColumnName("source")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.Property(x => x.RejectReason).HasColumnName("reject_reason").HasMaxLength(1000);
        builder.Property(x => x.IsHiddenByStudent).HasColumnName("is_hidden_by_student").HasDefaultValue(false);
        builder.Property(x => x.CvSnapshotUrl).HasColumnName("cv_snapshot_url").HasMaxLength(2048);
        builder.Property(x => x.UniversityId).HasColumnName("university_id");

        builder.Property(x => x.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamptz");
        builder.Property(x => x.ReviewedBy).HasColumnName("reviewed_by");

        builder.Property(x => x.InternPhaseId)
            .HasColumnName("intern_phase_id")
            .IsRequired(false);

        // ===== Audit columns =====
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne(x => x.Enterprise)
            .WithMany(e => e.InternshipApplications)
            .HasForeignKey(x => x.EnterpriseId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(x => x.Term)
            .WithMany(t => t.InternshipApplications)
            .HasForeignKey(x => x.TermId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(x => x.Student)
            .WithMany(s => s.InternshipApplications)
            .HasForeignKey(x => x.StudentId)
            .IsRequired(false);

        builder.HasOne(x => x.Job)
            .WithMany(j => j.InternshipApplications)
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(x => x.Reviewer)
            .WithMany(eu => eu.ReviewedApplications)
            .HasForeignKey(x => x.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(x => x.University)
            .WithMany()
            .HasForeignKey(x => x.UniversityId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // NOTE: Removed UNIQUE(StudentId, TermId, EnterpriseId) to support AC-10
        // A student can have both a SelfApply and a UniAssign application at the same enterprise in the same term.
        builder.HasIndex(x => new { x.StudentId, x.TermId, x.EnterpriseId });
        builder.HasIndex(x => new { x.EnterpriseId, x.Status });
        builder.HasIndex(x => new { x.StudentId, x.Status });
    }
}
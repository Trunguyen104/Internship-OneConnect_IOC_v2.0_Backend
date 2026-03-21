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

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.Property(x => x.RejectReason).HasColumnName("reject_reason").HasMaxLength(500);

        builder.Property(x => x.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamptz");
        builder.Property(x => x.ReviewedBy).HasColumnName("reviewed_by");

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

        builder.HasOne(x => x.Reviewer)
            .WithMany(eu => eu.ReviewedApplications)
            .HasForeignKey(x => x.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(x => new { x.StudentId, x.TermId, x.EnterpriseId }).IsUnique();
    }
}

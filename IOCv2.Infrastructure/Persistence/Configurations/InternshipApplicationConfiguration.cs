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
        builder.Property(x => x.InternshipId).HasColumnName("internship_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.Property(x => x.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamptz");
        builder.Property(x => x.ReviewedBy).HasColumnName("reviewed_by");

        builder.HasOne(x => x.InternshipGroup)
            .WithMany(ig => ig.InternshipApplications)
            .HasForeignKey(x => x.InternshipId)
            .IsRequired(false);

        builder.HasOne(x => x.Student)
            .WithMany(s => s.InternshipApplications)
            .HasForeignKey(x => x.StudentId)
            .IsRequired(false);

        builder.HasOne(x => x.Reviewer)
            .WithMany(eu => eu.ReviewedApplications)
            .HasForeignKey(x => x.ReviewedBy)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(x => new { x.InternshipId, x.StudentId }).IsUnique();
    }
}

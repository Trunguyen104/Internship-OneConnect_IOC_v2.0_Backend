using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("job_applications");

        // PK
        builder.HasKey(x => x.ApplicationId);
        builder.Property(x => x.ApplicationId)
               .HasColumnName("application_id")
               .ValueGeneratedOnAdd();

        // FKs
        builder.Property(x => x.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(x => x.StudentId).HasColumnName("student_id").IsRequired();
        builder.Property(x => x.CvId).HasColumnName("cv_id").IsRequired();

        // Fields
        builder.Property(x => x.CoverLetter)
               .HasColumnName("cover_letter")
               .HasColumnType("text")
               .IsRequired(false);

        builder.Property(x => x.Status)
               .HasColumnName("status")
               .HasConversion<short>()
               .HasColumnType("smallint")
               .IsRequired();

        builder.Property(x => x.AppliedAt)
               .HasColumnName("applied_at")
               .HasColumnType("timestamptz")
               .HasDefaultValueSql("now()")
               .IsRequired();

        // Audit fields (BaseEntity)
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");

        // Relationships
        builder.HasOne(x => x.Job)
               .WithMany(j => j.JobApplications)
               .HasForeignKey(x => x.JobId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_job_applications_jobs_job_id");

        builder.HasOne(x => x.Student)
               .WithMany(s => s.JobApplications)
               .HasForeignKey(x => x.StudentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_job_applications_students_student_id");

        // Indexes
        builder.HasIndex(x => x.JobId).HasDatabaseName("ix_job_applications_job_id");
        builder.HasIndex(x => x.StudentId).HasDatabaseName("ix_job_applications_student_id");
        builder.HasIndex(x => new { x.JobId, x.StudentId })
               .IsUnique()
               .HasDatabaseName("ix_job_applications_job_id_student_id");

        // Soft-delete global filter
        builder.HasQueryFilter(x => x.DeletedAt == null);
    }
}
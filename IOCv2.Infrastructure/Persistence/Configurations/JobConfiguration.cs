using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class JobConfiguration : IEntityTypeConfiguration<Job>
    {
        public void Configure(EntityTypeBuilder<Job> builder)
        {
            builder.ToTable("jobs");

        // PK
            builder.HasKey(j => j.JobId);
        builder.Property(j => j.JobId)
               .HasColumnName("job_id")
               .ValueGeneratedOnAdd();

        // FK
        builder.Property(j => j.EnterpriseId)
               .HasColumnName("enterprise_id")
               .IsRequired();

        builder.Property(j => j.PhaseId)
               .HasColumnName("phase_id")
               .IsRequired(false);

        builder.HasOne(j => j.Enterprise)
               .WithMany(e => e.Jobs)
               .HasForeignKey(j => j.EnterpriseId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_jobs_enterprises_enterprise_id");

        builder.HasOne(j => j.InternshipPhase)
               .WithMany(p => p.Jobs)
               .HasForeignKey(j => j.PhaseId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("fk_jobs_internship_phases_phase_id");

        // Fields
        builder.Property(j => j.Title)
               .HasColumnName("title")
               .HasMaxLength(255)
               .IsRequired(false);

        builder.Property(j => j.Position)
               .HasColumnName("position")
               .HasMaxLength(255)
               .IsRequired(false);

        builder.Property(j => j.Description)
               .HasColumnName("description")
               .HasColumnType("text")
               .IsRequired(false);

        builder.Property(j => j.Requirements)
               .HasColumnName("requirements")
               .HasColumnType("text")
               .IsRequired(false);

        builder.Property(j => j.Location)
               .HasColumnName("location")
               .HasMaxLength(255)
               .IsRequired(false);

        builder.Property(j => j.Benefit)
               .HasColumnName("benefit")
               .HasColumnType("text")
               .IsRequired(false);

        builder.Property(j => j.Quantity)
               .HasColumnName("quantity")
               .IsRequired(false);

        builder.Property(j => j.ExpireDate)
               .HasColumnName("expire_date")
               .HasColumnType("timestamp with time zone")
               .IsRequired(false);

        // New: internship date range
        builder.Property(j => j.StartDate)
               .HasColumnName("start_date")
               .HasColumnType("timestamp with time zone")
               .IsRequired(false);

        builder.Property(j => j.EndDate)
               .HasColumnName("end_date")
               .HasColumnType("timestamp with time zone")
               .IsRequired(false);

        // New: audience (public / targeted)
        builder.Property(j => j.Audience)
               .HasColumnName("audience")
               .HasConversion<short>()
               .IsRequired(false);

        // Enum stored as short (repository convention)
            builder.Property(j => j.Status)
                .HasColumnName("status")
                .HasConversion<short>()
               .IsRequired(false);

        // Audit fields
        builder.Property(j => j.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("now()")
               .IsRequired();

        builder.Property(j => j.CreatedBy)
               .HasColumnName("created_by");

        builder.Property(j => j.UpdatedAt)
               .HasColumnName("updated_at");

        builder.Property(j => j.UpdatedBy)
               .HasColumnName("updated_by");

        builder.Property(j => j.DeletedAt)
               .HasColumnName("deleted_at");

        // Indexes
        builder.HasIndex(j => j.EnterpriseId)
               .HasDatabaseName("ix_jobs_enterprise_id");

        builder.HasIndex(j => j.Status)
               .HasDatabaseName("ix_jobs_status");

        builder.HasIndex(j => j.PhaseId)
               .HasDatabaseName("ix_jobs_phase_id");

        // Many-to-many: jobs <-> universities via join table job_universities
        builder.HasMany(j => j.Universities)
               .WithMany(u => u.Jobs)
               .UsingEntity<Dictionary<string, object>>(
                   "job_universities",
                   ju => ju.HasOne<University>().WithMany().HasForeignKey("uni_id").HasConstraintName("fk_job_universities_university_id").OnDelete(DeleteBehavior.Cascade),
                   ju => ju.HasOne<Job>().WithMany().HasForeignKey("job_id").HasConstraintName("fk_job_universities_job_id").OnDelete(DeleteBehavior.Cascade),
                   je =>
                   {
                       je.HasKey("job_id", "uni_id");
                       je.ToTable("job_universities");
                       je.Property<Guid>("job_id").HasColumnName("job_id");
                       je.Property<Guid>("uni_id").HasColumnName("uni_id");
                       je.HasIndex(new[] { "uni_id" }).HasDatabaseName("ix_job_universities_uni_id");
                       je.HasIndex(new[] { "job_id" }).HasDatabaseName("ix_job_universities_job_id");
                   });

            builder.HasIndex(j => j.EnterpriseId).HasDatabaseName("ix_jobs_enterprise_id");
            builder.HasIndex(j => j.Status).HasDatabaseName("ix_jobs_status");
        }
    }
}

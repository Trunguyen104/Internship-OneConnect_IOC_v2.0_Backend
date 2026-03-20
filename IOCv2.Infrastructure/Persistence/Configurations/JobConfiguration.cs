using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Infrastructure.Persistence.Configurations;

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

        builder.HasOne(j => j.Enterprise)
               .WithMany(e => e.Job)
               .HasForeignKey(j => j.EnterpriseId)
               .OnDelete(DeleteBehavior.Cascade)
               .HasConstraintName("fk_jobs_enterprises_enterprise_id");

        // Fields
        builder.Property(j => j.Title)
               .HasColumnName("title")
               .HasMaxLength(255)
               .IsRequired();

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

        builder.Property(j => j.InternshipDuration)
               .HasColumnName("internship_duration")
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

        // Enum stored as short (repository convention)
        builder.Property(j => j.Status)
               .HasColumnName("status")
               .HasConversion<short>()
               .IsRequired();

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

        // Soft delete filter
        builder.HasQueryFilter(j => j.DeletedAt == null);
    }
}
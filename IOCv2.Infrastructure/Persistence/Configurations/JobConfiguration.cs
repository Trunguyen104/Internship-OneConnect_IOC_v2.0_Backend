using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class JobConfiguration : IEntityTypeConfiguration<Job>
    {
        public void Configure(EntityTypeBuilder<Job> builder)
        {
            builder.ToTable("jobs");
            builder.HasKey(j => j.JobId);
            builder.Property(j => j.JobId).HasColumnName("job_id");

            builder.Property(j => j.EnterpriseId).IsRequired().HasColumnName("enterprise_id");
            builder.Property(j => j.Title).IsRequired().HasMaxLength(255).HasColumnName("title");
            builder.Property(j => j.Description).HasColumnName("description");
            builder.Property(j => j.Requirements).HasColumnName("requirements");
            builder.Property(j => j.Location).HasMaxLength(255).HasColumnName("location");
            builder.Property(j => j.Benefit).HasColumnName("benefit");
            builder.Property(j => j.Quantity).HasColumnName("quantity");
            builder.Property(j => j.ExpireDate).HasColumnName("expire_date");
            builder.Property(j => j.Status)
                .HasColumnName("status")
                .HasConversion<short>()
                .HasColumnType("smallint");

            builder.Property(j => j.CreatedAt).HasColumnName("created_at");
            builder.Property(j => j.CreatedBy).HasColumnName("created_by");
            builder.Property(j => j.UpdatedAt).HasColumnName("updated_at");
            builder.Property(j => j.UpdatedBy).HasColumnName("updated_by");
            builder.Property(j => j.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(j => j.Enterprise)
                .WithMany(e => e.Jobs)
                .HasForeignKey(j => j.EnterpriseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(j => j.EnterpriseId).HasDatabaseName("ix_jobs_enterprise_id");
            builder.HasIndex(j => j.Status).HasDatabaseName("ix_jobs_status");
        }
    }
}

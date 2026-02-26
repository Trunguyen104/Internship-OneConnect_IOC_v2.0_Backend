using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("projects");

            // Primary Key
            builder.HasKey(x => x.ProjectId);
            builder.Property(x => x.ProjectId)
                .HasColumnName("project_id")
                .HasDefaultValueSql("gen_random_uuid()");

            // Properties
            builder.Property(x => x.InternshipId)
                .HasColumnName("internship_id")
                .IsRequired();

            builder.Property(x => x.ProjectName)
                .HasColumnName("project_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasMaxLength(2000);

            builder.Property(x => x.StartDate)
                .HasColumnName("start_date");

            builder.Property(x => x.EndDate)
                .HasColumnName("end_date");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<short>();

            // Audit fields from BaseEntity
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(x => x.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(x => x.CreatedBy)
                .HasColumnName("created_by");

            builder.Property(x => x.UpdatedBy)
                .HasColumnName("updated_by");

            // Indexes
            builder.HasIndex(x => x.InternshipId)
                .HasDatabaseName("ix_projects_internship_id");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("ix_projects_status");

            builder.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("ix_projects_created_at");

            builder.HasMany(x => x.ProjectResources)
                .WithOne(pr => pr.Project)
                .HasForeignKey(pr => pr.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

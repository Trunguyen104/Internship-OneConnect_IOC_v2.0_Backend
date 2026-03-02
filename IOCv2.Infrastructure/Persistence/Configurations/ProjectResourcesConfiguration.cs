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
    public class ProjectResourcesConfiguration : IEntityTypeConfiguration<ProjectResources>
    {
        public void Configure(EntityTypeBuilder<ProjectResources> builder)
        {
            builder.ToTable("project_resources");

            // Primary Key
            builder.HasKey(x => x.ProjectResourceId);
            builder.Property(x => x.ProjectResourceId)
                .HasColumnName("project_resource_id")
                .HasDefaultValueSql("gen_random_uuid()");

            // Properties
            builder.Property(x => x.ProjectId)
                .HasColumnName("project_id")
                .IsRequired();

            builder.Property(x => x.ResourceName)
                .HasColumnName("resource_name")
                .HasMaxLength(1000);

            builder.Property(x => x.ResourceType)
                .HasColumnName("resource_type")
                .HasMaxLength(50);

            builder.Property(x => x.ResourceUrl)
                .HasColumnName("resource_url")
                .HasMaxLength(512);

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
            builder.HasIndex(x => x.ProjectId)
                .HasDatabaseName("ix_project_resources_project_id");

            builder.HasIndex(x => x.ResourceType)
                .HasDatabaseName("ix_project_resources_resource_type");

            builder.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("ix_project_resources_created_at");

            // Relationships
            builder.HasOne(x => x.Project)
                .WithMany(p => p.ProjectResources)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

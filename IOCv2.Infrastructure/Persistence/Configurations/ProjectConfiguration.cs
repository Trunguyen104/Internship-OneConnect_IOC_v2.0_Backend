using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
                .IsRequired(false);

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

            // Explicitly ignore the legacy Status property (replaced by two-layer model)
            builder.Ignore(x => x.Status);

            builder.Property(x => x.VisibilityStatus)
                .HasColumnName("visibility_status")
                .HasConversion<short>()
                .HasDefaultValue(VisibilityStatus.Draft)
                .IsRequired();

            builder.Property(x => x.OperationalStatus)
                .HasColumnName("operational_status")
                .HasConversion<short>()
                .HasDefaultValue(OperationalStatus.Unstarted)
                .IsRequired();

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

            // FK
            builder.HasOne(p => p.InternshipGroup)
                .WithMany(g => g.Projects)
                .HasForeignKey(p => p.InternshipId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            builder.HasIndex(x => x.InternshipId)
                .HasDatabaseName("ix_projects_internship_id");

            builder.HasIndex(x => x.VisibilityStatus)
                .HasDatabaseName("ix_projects_visibility_status");

            builder.HasIndex(x => x.OperationalStatus)
                .HasDatabaseName("ix_projects_operational_status");

            builder.HasIndex(x => x.CreatedAt)
                .HasDatabaseName("ix_projects_created_at");

            builder.HasMany(x => x.ProjectResources)
                .WithOne(pr => pr.Project)
                .HasForeignKey(pr => pr.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

        // New fields
        builder.Property(x => x.MentorId)
            .HasColumnName("mentor_id");

        builder.Property(x => x.ProjectCode)
            .HasColumnName("project_code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Field)
            .HasColumnName("field")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Template)
            .HasColumnName("template")
            .HasConversion<short>()
            .HasDefaultValue(ProjectTemplate.None);

        builder.Property(x => x.Requirements)
            .HasColumnName("requirements")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.Deliverables)
            .HasColumnName("deliverables")
            .HasMaxLength(2000);

        // F4: IsOrphaned flag (AC-16) — phân biệt "chưa gán nhóm" vs "nhóm bị xóa"
        builder.Property(x => x.IsOrphaned)
            .HasColumnName("is_orphaned")
            .HasDefaultValue(false)
            .IsRequired();

        // FK: MentorId → enterprise_users ON DELETE SET NULL
        builder.HasOne<EnterpriseUser>()
            .WithMany()
            .HasForeignKey(p => p.MentorId)
            .OnDelete(DeleteBehavior.SetNull);


        // Unique partial index: project_code WHERE deleted_at IS NULL
        builder.HasIndex(x => x.ProjectCode)
            .HasDatabaseName("uix_projects_project_code_active")
            .HasFilter("deleted_at IS NULL")
            .IsUnique();
        }
    }
}

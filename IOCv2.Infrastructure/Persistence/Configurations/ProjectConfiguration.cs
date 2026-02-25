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
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(p => p.InternshipId)
                .HasColumnName("internship_id")
                .IsRequired();

            builder.Property(p => p.MentorId)
                .HasColumnName("mentor_id");

            builder.Property(p => p.ProjectName)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("project_name");

            builder.Property(p => p.Description)
                .HasColumnName("description");

            builder.Property(p => p.StartDate)
                .HasColumnName("start_date");

            builder.Property(p => p.EndDate)
                .HasColumnName("end_date");

            builder.Property(p => p.Status)
                .HasConversion<short>()
                .HasDefaultValue(ProjectStatus.Planning)
                .HasColumnName("status");

            builder.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()");

            builder.Property(p => p.CreatedBy)
                .HasColumnName("created_by");

            builder.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(p => p.UpdatedBy)
                .HasColumnName("updated_by");

            builder.Property(p => p.DeletedAt)
                .HasColumnName("deleted_at");

            builder.HasMany(p => p.Stakeholders)
                .WithOne(s => s.Project)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

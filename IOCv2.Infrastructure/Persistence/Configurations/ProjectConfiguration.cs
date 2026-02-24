using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.InternshipId)
            .IsRequired();
        
        builder.Property(p => p.MentorId);
        
        builder.Property(p => p.ProjectName)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(p => p.Description);
        
        builder.Property(p => p.StartDate);
        
        builder.Property(p => p.EndDate);
        
        builder.Property(p => p.Status)
            .IsRequired()
            .HasDefaultValue(ProjectStatus.Planning);
        
        builder.Property(p => p.CreatedAt)
            .IsRequired();
        
        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100);
        
        builder.Property(p => p.UpdatedAt);
        
        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100);
        
        builder.Property(p => p.DeletedAt);
        
        // Navigation properties
        builder.HasMany(p => p.Stakeholders)
            .WithOne(s => s.Project)
            .HasForeignKey(s => s.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}


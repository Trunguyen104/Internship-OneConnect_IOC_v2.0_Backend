using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("work_items");

        builder.HasKey(e => e.WorkItemId);

        builder.Property(e => e.WorkItemId)
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasMaxLength(255);

        builder.HasOne(e => e.Project)
            .WithMany(p => p.WorkItems)
            .HasForeignKey(e => e.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Parent)
            .WithMany(w => w.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Assignee)
            .WithMany(m => m.AssignedWorkItems)
            .HasForeignKey(e => e.AssigneeProjectMemberId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class SprintWorkItemConfiguration : IEntityTypeConfiguration<SprintWorkItem>
{
    public void Configure(EntityTypeBuilder<SprintWorkItem> builder)
    {
        builder.ToTable("sprint_work_items");
        
        builder.HasKey(swi => swi.SprintWorkItemId);
        
        builder.Property(swi => swi.SprintWorkItemId)
            .HasColumnName("sprint_work_item_id")
            .IsRequired();
        
        builder.Property(swi => swi.SprintId)
            .HasColumnName("sprint_id")
            .IsRequired();
        
        builder.Property(swi => swi.WorkItemId)
            .HasColumnName("work_item_id")
            .IsRequired();
        
        builder.Property(swi => swi.BoardOrder)
            .HasColumnName("board_order")
            .IsRequired();
        
        // Base entity properties
        builder.Property(swi => swi.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
        
        builder.Property(swi => swi.UpdatedAt)
            .HasColumnName("updated_at");
        
        builder.Property(swi => swi.DeletedAt)
            .HasColumnName("deleted_at");
        
        builder.Property(swi => swi.CreatedBy)
            .HasColumnName("created_by");
        
        builder.Property(swi => swi.UpdatedBy)
            .HasColumnName("updated_by");
        
        // Relationships
        builder.HasOne(swi => swi.Sprint)
            .WithMany(s => s.SprintWorkItems)
            .HasForeignKey(swi => swi.SprintId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(swi => swi.WorkItem)
            .WithMany()
            .HasForeignKey(swi => swi.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Unique constraint: One WorkItem can only be in a Sprint once
        builder.HasIndex(swi => new { swi.SprintId, swi.WorkItemId })
            .IsUnique()
            .HasDatabaseName("ix_sprint_work_items_unique");
        
        // Index for board ordering
        builder.HasIndex(swi => new { swi.SprintId, swi.BoardOrder })
            .HasDatabaseName("ix_sprint_work_items_board_order");
    }
}

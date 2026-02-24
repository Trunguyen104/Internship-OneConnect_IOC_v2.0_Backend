using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class WorkItemConfiguration : IEntityTypeConfiguration<WorkItem>
{
    public void Configure(EntityTypeBuilder<WorkItem> builder)
    {
        builder.ToTable("work_items");
        
        // Primary Key
        builder.HasKey(w => w.WorkItemId);
        builder.Property(w => w.WorkItemId)
               .HasColumnName("work_item_id")
               .ValueGeneratedOnAdd();
        
        // Foreign Keys
        builder.Property(w => w.ProjectId)
               .HasColumnName("project_id")
               .IsRequired();
        
        builder.Property(w => w.ParentId)
               .HasColumnName("parent_id")
               .IsRequired(false);
        
        // Self-referencing relationship
        builder.HasOne(w => w.Parent)
               .WithMany(w => w.Children)
               .HasForeignKey(w => w.ParentId)
               .OnDelete(DeleteBehavior.Restrict);
        
        // Enums - stored as short
        builder.Property(w => w.Type)
               .HasColumnName("type")
               .HasConversion<short>()
               .IsRequired();
        
        builder.Property(w => w.Priority)
               .HasColumnName("priority")
               .HasConversion<short>()
               .IsRequired(false);
        
        builder.Property(w => w.Status)
               .HasColumnName("status")
               .HasConversion<short>()
               .IsRequired(false);
        
        // Required fields
        builder.Property(w => w.Title)
               .HasColumnName("title")
               .HasMaxLength(255)
               .IsRequired();
        
        builder.Property(w => w.Description)
               .HasColumnName("description")
               .HasColumnType("text")
               .IsRequired(false);
        
        // Nullable fields (for Story/Task/Subtask only)
        builder.Property(w => w.StoryPoint)
               .HasColumnName("story_point")
               .IsRequired(false);
        
        builder.Property(w => w.StartDate)
               .HasColumnName("start_date")
               .IsRequired(false);
        
        builder.Property(w => w.DueDate)
               .HasColumnName("due_date")
               .IsRequired(false);
        
        builder.Property(w => w.BacklogOrder)
               .HasColumnName("backlog_order")
               .HasDefaultValue(0)
               .IsRequired();
        
        builder.Property(w => w.OriginalEstimate)
               .HasColumnName("original_estimate")
               .IsRequired(false);
        
        builder.Property(w => w.RemainingWork)
               .HasColumnName("remaining_work")
               .IsRequired(false);
        
        // Base Entity fields from BaseEntity
        builder.Property(w => w.CreatedAt)
               .HasColumnName("created_at")
               .HasDefaultValueSql("now()")
               .IsRequired();
        
        builder.Property(w => w.CreatedBy)
               .HasColumnName("created_by")
               .HasMaxLength(100);
        
        builder.Property(w => w.UpdatedAt)
               .HasColumnName("updated_at");
        
        builder.Property(w => w.UpdatedBy)
               .HasColumnName("updated_by")
               .HasMaxLength(100);
        
        builder.Property(w => w.DeletedAt)
               .HasColumnName("deleted_at");
        
        // Indexes
        builder.HasIndex(w => w.ProjectId)
               .HasDatabaseName("ix_work_items_project_id");
        
        builder.HasIndex(w => w.ParentId)
               .HasDatabaseName("ix_work_items_parent_id");
        
        builder.HasIndex(w => w.Type)
               .HasDatabaseName("ix_work_items_type");
        
        builder.HasIndex(w => w.Status)
               .HasDatabaseName("ix_work_items_status");
        
        builder.HasIndex(w => w.BacklogOrder)
               .HasDatabaseName("ix_work_items_backlog_order");
        
        // Global query filter for soft delete
        builder.HasQueryFilter(w => w.DeletedAt == null);
    }
}

using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class SprintWorkItemConfiguration : IEntityTypeConfiguration<SprintWorkItem>
{
    public void Configure(EntityTypeBuilder<SprintWorkItem> builder)
    {
        builder.ToTable("sprint_work_items");

        // Composite Primary Key
        builder.HasKey(swi => new { swi.SprintId, swi.WorkItemId });

        builder.Property(swi => swi.SprintId)
            .HasColumnName("sprint_id")
            .IsRequired();

        builder.Property(swi => swi.WorkItemId)
            .HasColumnName("work_item_id")
            .IsRequired();

        builder.Property(swi => swi.BoardOrder)
            .HasColumnName("board_order")
            .HasColumnType("real")
            .IsRequired();

        // Relationships
        builder.HasOne(swi => swi.Sprint)
            .WithMany(s => s.SprintWorkItems)
            .HasForeignKey(swi => swi.SprintId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_sprint_work_items_sprints_sprint_id")
            .IsRequired(false);

        builder.HasOne(swi => swi.WorkItem)
            .WithMany()
            .HasForeignKey(swi => swi.WorkItemId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_sprint_work_items_work_items_work_item_id");

        // Index for board ordering
        builder.HasIndex(swi => new { swi.SprintId, swi.BoardOrder })
            .HasDatabaseName("ix_sprint_work_items_board_order");
    }
}

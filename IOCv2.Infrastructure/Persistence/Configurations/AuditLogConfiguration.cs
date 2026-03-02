using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");

            builder.HasKey(x => x.AuditLogId);

            builder.Property(x => x.AuditLogId).HasColumnName("log_id");
            builder.Property(x => x.Action).HasColumnName("action").IsRequired(); // Kept IsRequired from original
            builder.Property(x => x.EntityType).HasColumnName("entity_type").IsRequired().HasMaxLength(50);
            builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
            builder.Property(x => x.PerformedById).HasColumnName("performed_by");
            builder.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(500);
            builder.Property(x => x.Metadata).HasColumnName("metadata").HasColumnType("jsonb");

            builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            builder.HasOne(x => x.PerformedBy)
                .WithMany(e => e.PerformedLogs)
                .HasForeignKey(x => x.PerformedById)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.HasIndex(x => new { x.EntityType, x.EntityId });
            builder.HasIndex(x => x.PerformedById);
        }
    }
}

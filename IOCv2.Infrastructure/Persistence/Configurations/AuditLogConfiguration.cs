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

            builder.Property(x => x.Action)
                .IsRequired();

            builder.Property(x => x.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.EntityId)
                .IsRequired();

            builder.Property(x => x.Reason)
                .HasMaxLength(500);

            builder.Property(x => x.Metadata)
                .HasColumnType("jsonb");

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("now()");

            builder.HasOne(x => x.PerformedBy)
                .WithMany(e => e.PerformedLogs)
                .HasForeignKey(x => x.PerformedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.EntityType, x.EntityId });
            builder.HasIndex(x => x.PerformedById);
        }
    }
}

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

            builder.HasKey(x => x.LogId);

            builder.Property(x => x.Action)
                .IsRequired();

            builder.Property(x => x.TargetId)
                .IsRequired();

            builder.Property(x => x.Reason)
                .HasMaxLength(500);

            builder.Property(x => x.Metadata)
                .HasColumnType("jsonb"); // PostgreSQL jsonb for flexible metadata

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Target)
                .WithMany(e => e.TargetLogs)
                .HasForeignKey(x => x.TargetId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.PerformedBy)
                .WithMany(e => e.PerformedLogs)
                .HasForeignKey(x => x.PerformedUserById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using IOCv2.Domain.Entities;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");
            builder.HasKey(al => al.LogId);
            builder.Property(al => al.Action).IsRequired();
            builder.Property(al => al.TargetId).IsRequired();
            builder.Property(al => al.PerformedUserById).IsRequired();
            builder.Property(al => al.Reason).HasMaxLength(500);
            builder.Property(al => al.Metadata).HasColumnType("jsonb");
            builder.Property(al => al.CreatedAt).IsRequired();
            builder.HasOne(al => al.Target)
                   .WithMany()
                   .HasForeignKey(al => al.TargetId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(al => al.PerformedBy).
                WithMany(e => e.PeformedLogs).
                HasForeignKey(al => al.PerformedUserById).
                OnDelete(DeleteBehavior.Restrict);
        }
    }
}

using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.ToTable("application_status_histories");

        builder.HasKey(x => x.HistoryId);

        builder.Property(x => x.HistoryId).HasColumnName("history_id");
        builder.Property(x => x.ApplicationId).HasColumnName("application_id").IsRequired();

        builder.Property(x => x.FromStatus)
            .HasColumnName("from_status")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.Property(x => x.ToStatus)
            .HasColumnName("to_status")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.Property(x => x.Note).HasColumnName("note").HasMaxLength(1000);
        builder.Property(x => x.ChangedByName).HasColumnName("changed_by_name").HasMaxLength(200);
        builder.Property(x => x.TriggerSource).HasColumnName("trigger_source").HasMaxLength(50).HasDefaultValue("HR");

        // ===== Audit columns =====
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne(x => x.Application)
            .WithMany(a => a.StatusHistories)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ApplicationId);
        builder.HasIndex(x => x.CreatedAt);
    }
}

using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class ViolationReportConfiguration : IEntityTypeConfiguration<ViolationReport>
    {
        public void Configure(EntityTypeBuilder<ViolationReport> builder)
        {
            // Tên bảng: snake_case, số nhiều
            builder.ToTable("violation_reports");

            // Khóa chính
            builder.HasKey(x => x.ViolationReportId);
            builder.Property(x => x.ViolationReportId)
                   .HasColumnName("violation_report_id");

            // Các thuộc tính
            builder.Property(x => x.Description)
                   .IsRequired()
                   .HasMaxLength(2000)
                   .HasColumnName("description");

            builder.Property(x => x.OccurredDate)
                   .IsRequired()
                   .HasColumnName("occurred_date");

            builder.Property(x => x.Status)
                   .IsRequired()
                   .HasConversion<int>()
                   .HasColumnName("status");

            builder.Property(x => x.Type)
                   .IsRequired()
                   .HasConversion<int>()
                   .HasColumnName("type");

            builder.Property(x => x.Severity)
                   .IsRequired()
                   .HasConversion<int>()
                   .HasColumnName("severity");

            // Audit fields (kế thừa từ BaseEntity)
            builder.Property(x => x.CreatedAt)
                   .IsRequired()
                   .HasColumnName("created_at");

            builder.Property(x => x.UpdatedAt)
                   .HasColumnName("updated_at");

            builder.Property(x => x.DeletedAt)
                   .HasColumnName("deleted_at");

            builder.Property(x => x.CreatedBy)
                   .HasColumnName("created_by");

            builder.Property(x => x.UpdatedBy)
                   .HasColumnName("updated_by");

            // Relationships
            builder.HasOne(x => x.Student)
                   .WithMany(s => s.ViolationReports)
                   .HasForeignKey(x => x.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.InternshipGroup)
                   .WithMany(g => g.ViolationReports)
                   .HasForeignKey(x => x.InternshipGroupId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Attachments)
                   .WithOne(a => a.ViolationReport)
                   .HasForeignKey(a => a.ViolationReportId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Comments)
                   .WithOne(c => c.ViolationReport)
                   .HasForeignKey(c => c.ViolationReportId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.UpdateHistories)
                   .WithOne(h => h.ViolationReport)
                   .HasForeignKey(h => h.ViolationReportId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Indexes để tối ưu truy vấn
            builder.HasIndex(x => x.StudentId)
                   .HasDatabaseName("ix_violation_reports_student_id");

            builder.HasIndex(x => x.InternshipGroupId)
                   .HasDatabaseName("ix_violation_reports_internship_group_id");

            builder.HasIndex(x => x.Status)
                   .HasDatabaseName("ix_violation_reports_status");

            builder.HasIndex(x => x.OccurredDate)
                   .HasDatabaseName("ix_violation_reports_occurred_date");
        }
    }
}
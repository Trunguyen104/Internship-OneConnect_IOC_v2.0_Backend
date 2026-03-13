using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class ViolationUpdateHistoryConfiguration : IEntityTypeConfiguration<ViolationUpdateHistory>
    {
        public void Configure(EntityTypeBuilder<ViolationUpdateHistory> builder)
        {
            // Tên bảng: snake_case, số nhiều
            builder.ToTable("violation_update_histories");

            // Khóa chính
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            // Các thuộc tính
            builder.Property(e => e.ViolationReportId)
                .IsRequired()
                .HasColumnName("violation_report_id");

            builder.Property(e => e.OldStatus)
                .HasConversion<short>()
                .IsRequired()
                .HasColumnName("old_status");

            builder.Property(e => e.NewStatus)
                .HasConversion<short>()
                .IsRequired()
                .HasColumnName("new_status");

            builder.Property(e => e.Reason)
                .IsRequired()
                .HasMaxLength(500)          // Điều chỉnh độ dài phù hợp với nghiệp vụ
                .HasColumnName("reason");

            // Audit columns (kế thừa từ BaseEntity)
            builder.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnName("created_at");

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(e => e.CreatedBy)
                .HasColumnName("created_by");

            builder.Property(e => e.UpdatedBy)
                .HasColumnName("updated_by");

            // Index cho khóa ngoại
            builder.HasIndex(e => e.ViolationReportId)
                .HasDatabaseName("ix_violation_update_histories_violation_report_id");

            // Relationship với ViolationReport
            builder.HasOne(e => e.ViolationReport)
                .WithMany() // Giả sử ViolationReport chưa có collection navigation. Nếu có, sửa thành .WithMany(vr => vr.ViolationUpdateHistories)
                .HasForeignKey(e => e.ViolationReportId)
                .OnDelete(DeleteBehavior.Restrict); // Hoặc Cascade tùy nghiệp vụ
        }
    }
}

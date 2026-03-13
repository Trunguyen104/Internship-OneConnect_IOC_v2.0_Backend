using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class ViolationAttachmentConfiguration : IEntityTypeConfiguration<ViolationAttachment>
    {
        public void Configure(EntityTypeBuilder<ViolationAttachment> builder)
        {
            // Tên bảng: snake_case, số nhiều
            builder.ToTable("violation_attachments");

            // Khóa chính
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                   .HasColumnName("id");

            // Các thuộc tính
            builder.Property(x => x.ViolationReportId)
                   .IsRequired()
                   .HasColumnName("violation_report_id");

            builder.Property(x => x.FilePath)
                   .IsRequired()
                   .HasMaxLength(500) // Độ dài phù hợp với đường dẫn file
                   .HasColumnName("file_path");

            builder.Property(x => x.FileName)
                   .IsRequired()
                   .HasMaxLength(255) // Tên file
                   .HasColumnName("file_name");

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
            builder.HasOne(x => x.ViolationReport)
                   .WithMany(vr => vr.Attachments) // Giả sử ViolationReport có collection Attachments
                   .HasForeignKey(x => x.ViolationReportId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa report thì xóa attachment

            // Indexes
            builder.HasIndex(x => x.ViolationReportId)
                   .HasDatabaseName("ix_violation_attachments_violation_report_id");
        }
    }
}
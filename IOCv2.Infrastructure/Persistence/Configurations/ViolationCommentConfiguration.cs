using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class ViolationCommentConfiguration : IEntityTypeConfiguration<ViolationComment>
    {
        public void Configure(EntityTypeBuilder<ViolationComment> builder)
        {
            // Tên bảng: snake_case, số nhiều
            builder.ToTable("violation_comments");

            // Khóa chính
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                   .HasColumnName("id");

            // Các thuộc tính
            builder.Property(x => x.ViolationReportId)
                   .IsRequired()
                   .HasColumnName("violation_report_id");

            builder.Property(x => x.UserId)
                   .IsRequired()
                   .HasColumnName("user_id");

            builder.Property(x => x.Content)
                   .IsRequired()
                   .HasMaxLength(1000) // Điều chỉnh độ dài phù hợp với nghiệp vụ
                   .HasColumnName("content");

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
                   .WithMany(vr => vr.Comments) // Giả sử ViolationReport có collection Comments
                   .HasForeignKey(x => x.ViolationReportId)
                   .OnDelete(DeleteBehavior.Cascade); // Xóa report thì xóa comment

            builder.HasOne<User>() // Liên kết với entity User (giả định có sẵn)
                   .WithMany()      // Nếu User có collection comments, sửa thành .WithMany(u => u.Comments)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Restrict); // Hạn chế xóa user khi còn comment

            // Indexes để tối ưu truy vấn
            builder.HasIndex(x => x.ViolationReportId)
                   .HasDatabaseName("ix_violation_comments_violation_report_id");

            builder.HasIndex(x => x.UserId)
                   .HasDatabaseName("ix_violation_comments_user_id");

            builder.HasIndex(x => x.CreatedAt)
                   .HasDatabaseName("ix_violation_comments_created_at");
        }
    }
}
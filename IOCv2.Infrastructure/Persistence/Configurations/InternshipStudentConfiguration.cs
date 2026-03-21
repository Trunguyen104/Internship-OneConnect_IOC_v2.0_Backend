using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class InternshipStudentConfiguration : IEntityTypeConfiguration<InternshipStudent>
    {
        public void Configure(EntityTypeBuilder<InternshipStudent> builder)
        {
            builder.ToTable("internship_students");

            builder.HasKey(e => new { e.InternshipId, e.StudentId });

            builder.HasOne(e => e.InternshipGroup)
                .WithMany(ig => ig.Members)
                .HasForeignKey(e => e.InternshipId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Student)
                .WithMany(s => s.InternshipStudents)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(e => e.Role)
                .HasColumnName("role")
                .HasConversion<short>()
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .HasConversion<short>()
                .IsRequired();

            builder.Property(e => e.JoinedAt)
                .HasColumnName("joined_at")
                .IsRequired();

            // ===== Audit columns =====
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
        }
    }
}

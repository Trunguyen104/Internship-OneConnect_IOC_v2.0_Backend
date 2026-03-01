using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.ToTable("students");
            builder.HasKey(s => s.StudentId);
            builder.Property(s => s.StudentId).HasColumnName("student_id");

            builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();
            builder.HasIndex(s => s.UserId).IsUnique();

            builder.Property(s => s.ClassName).HasMaxLength(50).HasColumnName("class_name");
            builder.Property(s => s.Major).HasMaxLength(100).HasColumnName("major");
            builder.Property(s => s.Gpa).HasPrecision(3, 2).HasColumnName("gpa");
            builder.Property(s => s.HighestDegree).HasMaxLength(100).HasColumnName("highest_degree");

            builder.Property(s => s.InternshipStatus)
                .HasConversion<short>()
                .HasColumnType("smallint")
                .HasColumnName("internship_status");

            builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
            builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            builder.Property(s => s.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
            builder.Property(s => s.CreatedBy).HasColumnName("created_by");
            builder.Property(s => s.UpdatedBy).HasColumnName("updated_by");

            builder.HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        }
    }
}

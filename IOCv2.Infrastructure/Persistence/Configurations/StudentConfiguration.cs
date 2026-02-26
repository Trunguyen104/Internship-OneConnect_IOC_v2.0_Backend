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

            builder.Property(s => s.Class).HasMaxLength(50).HasColumnName("class");
            builder.Property(s => s.Major).HasMaxLength(100).HasColumnName("major");
            builder.Property(s => s.Gpa).HasPrecision(3, 2).HasColumnName("gpa");
            builder.Property(s => s.HighestDegree).HasMaxLength(100).HasColumnName("highest_degree");

            builder.Property(s => s.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasColumnName("status");

            builder.HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Logbooks)
                .WithOne(l => l.Student)
                .HasForeignKey(l => l.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

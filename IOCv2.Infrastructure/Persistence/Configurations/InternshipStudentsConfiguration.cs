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
    public class InternshipStudentsConfiguration : IEntityTypeConfiguration<InternshipStudents>
    {
        public void Configure(EntityTypeBuilder<InternshipStudents> builder)
        {
            builder.ToTable("internship_students");

            // Composite PK
            builder.HasKey(x => new { x.InternshipId, x.StudentId });

            builder.Property(x => x.InternshipId)
                .HasColumnName("internship_id")
                .IsRequired();

            builder.Property(x => x.StudentId)
                .HasColumnName("student_id")
                .IsRequired();

            // Enum -> smallint
            builder.Property(x => x.Role)
                .HasColumnName("role")
                .HasConversion<short>()
                .HasColumnType("smallint");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<short>()
                .HasColumnType("smallint");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("joined_at") // keeping the column name as joined_at for domain meaning
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
            builder.Property(x => x.CreatedBy).HasColumnName("created_by");
            builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

            // Relationships
            builder.HasOne(x => x.InternshipGroup)
                .WithMany(ig => ig.InternshipStudents)
                .HasForeignKey(x => x.InternshipId)
                .IsRequired(false);

            builder.HasOne(x => x.Student)
                .WithMany(s => s.InternshipGroups)
                .HasForeignKey(x => x.StudentId)
                .IsRequired(false);
        }
    }
}

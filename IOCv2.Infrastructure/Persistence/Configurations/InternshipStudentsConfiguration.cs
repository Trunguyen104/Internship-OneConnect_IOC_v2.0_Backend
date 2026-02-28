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

            builder.Property(x => x.JoinedAt)
                .HasColumnName("joined_at")
                .HasColumnType("timestamptz")
                .HasDefaultValueSql("now()")
                .IsRequired();
        }
    }
}

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
        }
    }
}

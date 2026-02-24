using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class InternshipConfiguration : IEntityTypeConfiguration<Internship>
{
    public void Configure(EntityTypeBuilder<Internship> builder)
    {
        builder.ToTable("internships");

        builder.HasKey(e => e.InternshipId);

        builder.Property(e => e.InternshipId)
            .ValueGeneratedNever();

        builder.HasOne(e => e.Term)
            .WithMany(t => t.Internships)
            .HasForeignKey(e => e.TermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Job)
            .WithMany(j => j.Internships)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Mentor)
            .WithMany()
            .HasForeignKey(e => e.MentorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

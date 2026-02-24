using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");

        builder.HasKey(e => e.ProjectId);

        builder.Property(e => e.ProjectId)
            .ValueGeneratedNever();

        builder.Property(e => e.ProjectName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.Field)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(e => e.Internship)
            .WithMany(i => i.Projects)
            .HasForeignKey(e => e.InternshipId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Mentor)
            .WithMany()
            .HasForeignKey(e => e.MentorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

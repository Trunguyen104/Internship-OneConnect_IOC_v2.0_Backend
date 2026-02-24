using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(e => e.JobId);

        builder.Property(e => e.JobId)
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasMaxLength(255);

        builder.Property(e => e.Location)
            .HasMaxLength(500);

        builder.HasOne(e => e.Enterprise)
            .WithMany()
            .HasForeignKey(e => e.EnterpriseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

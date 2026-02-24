using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class TermConfiguration : IEntityTypeConfiguration<Term>
{
    public void Configure(EntityTypeBuilder<Term> builder)
    {
        builder.ToTable("terms");

        builder.HasKey(e => e.TermId);

        builder.Property(e => e.TermId)
            .ValueGeneratedNever();

        builder.Property(e => e.Name)
            .HasMaxLength(255);

        builder.HasOne(e => e.University)
            .WithMany()
            .HasForeignKey(e => e.UniversityId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

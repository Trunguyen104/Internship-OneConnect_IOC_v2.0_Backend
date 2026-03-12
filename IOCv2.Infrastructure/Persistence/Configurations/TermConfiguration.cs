using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class TermConfiguration : IEntityTypeConfiguration<Term>
{
    public void Configure(EntityTypeBuilder<Term> builder)
    {
        builder.ToTable("terms");

        builder.HasKey(x => x.TermId);

        builder.Property(x => x.TermId)
            .HasColumnName("term_id");

        builder.Property(x => x.UniversityId)
            .HasColumnName("university_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100);

        builder.Property(x => x.StartDate)
            .HasColumnName("start_date");

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.Property(x => x.CloseReason)
            .HasColumnName("close_reason")
            .HasMaxLength(500);

        builder.HasOne(x => x.University)
            .WithMany(u => u.Terms)
            .HasForeignKey(x => x.UniversityId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}

using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class LogbookConfiguration : IEntityTypeConfiguration<Logbook>
{
    public void Configure(EntityTypeBuilder<Logbook> builder)
    {
        builder.ToTable("logbooks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InternshipId).HasColumnName("internship_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.Content).HasColumnName("content").HasColumnType("text");
        
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.HasOne(x => x.InternshipGroup)
            .WithMany(ig => ig.Logbooks)
            .HasForeignKey(x => x.InternshipId)
            .IsRequired(false);

        builder.HasOne(x => x.Student)
            .WithMany(s => s.Logbooks)
            .HasForeignKey(x => x.StudentId)
            .IsRequired(false);
    }
}

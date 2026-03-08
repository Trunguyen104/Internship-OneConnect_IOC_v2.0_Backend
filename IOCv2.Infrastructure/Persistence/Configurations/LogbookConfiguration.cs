using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Generic;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class LogbookConfiguration : IEntityTypeConfiguration<Logbook>
{
    public void Configure(EntityTypeBuilder<Logbook> builder)
    {
        builder.ToTable("logbooks");

        builder.HasKey(x => x.LogbookId);
        builder.Property(x => x.LogbookId).HasColumnName("logbook_id").ValueGeneratedOnAdd();
        builder.Property(x => x.InternshipId).HasColumnName("internship_id");
        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.DateReport).HasColumnName("date_report");
        builder.Property(x => x.Summary).HasColumnName("summary").HasColumnType("text");
        builder.Property(x => x.Issue).HasColumnName("issue").HasColumnType("text");
        builder.Property(x => x.Plan).HasColumnName("plan").HasColumnType("text");
        
        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint");

        builder.HasOne(x => x.Internship)
            .WithMany(i => i.Logbooks)
            .HasForeignKey(x => x.InternshipId);

        builder.HasOne(x => x.Student)
            .WithMany(s => s.Logbooks)
            .HasForeignKey(x => x.StudentId);

        builder.HasMany(x => x.WorkItems)

            .WithMany()
            .UsingEntity(j => j.ToTable("logbook_work_items"));
    }
}

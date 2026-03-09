using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.StartDate)
            .HasColumnName("start_date")
            .IsRequired();

        builder.Property(x => x.EndDate)
            .HasColumnName("end_date")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .HasColumnType("smallint")
            .IsRequired()
            .HasDefaultValue(TermStatus.Open);

        builder.Property(x => x.Version)
            .HasColumnName("version")
            .IsRequired()
            .HasDefaultValue(1)
            .IsConcurrencyToken();

        builder.Property(x => x.TotalEnrolled)
            .HasColumnName("total_enrolled")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalPlaced)
            .HasColumnName("total_placed")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalUnplaced)
            .HasColumnName("total_unplaced")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.ClosedBy)
            .HasColumnName("closed_by");

        builder.Property(x => x.ClosedAt)
            .HasColumnName("closed_at")
            .HasColumnType("timestamptz");

        // BaseEntity properties
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired()
            .HasDefaultValueSql("now()");

        builder.Property(x => x.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz");

        builder.Property(x => x.UpdatedBy)
            .HasColumnName("updated_by");

        builder.Property(x => x.DeletedAt)
            .HasColumnName("deleted_at")
            .HasColumnType("timestamptz");

        builder.Property(x => x.DeletedBy)
            .HasColumnName("deleted_by");

        // Relationships
        builder.HasOne(x => x.University)
            .WithMany(u => u.Terms)
            .HasForeignKey(x => x.UniversityId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.UniversityId)
            .HasDatabaseName("ix_terms_university_id");

        builder.HasIndex(x => new { x.UniversityId, x.Name })
            .IsUnique()
            .HasDatabaseName("ix_terms_university_id_name");

        builder.HasIndex(x => new { x.StartDate, x.EndDate })
            .HasDatabaseName("ix_terms_start_date_end_date");
    }
}

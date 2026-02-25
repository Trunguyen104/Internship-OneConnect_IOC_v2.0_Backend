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
    public class TermConfiguration : IEntityTypeConfiguration<Term>
    {
        public void Configure(EntityTypeBuilder<Term> builder)
        {
            builder.ToTable("terms");

            // Primary Key
            builder.HasKey(x => x.TermId);
            builder.Property(x => x.TermId)
                .HasColumnName("term_id")
                .HasDefaultValueSql("gen_random_uuid()");

            // Properties
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
                .HasConversion<short>();

            // Audit fields from BaseEntity
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(x => x.DeletedAt)
                .HasColumnName("deleted_at");

            builder.Property(x => x.CreatedBy)
                .HasColumnName("created_by");

            builder.Property(x => x.UpdatedBy)
                .HasColumnName("updated_by");

            // Indexes
            builder.HasIndex(x => x.UniversityId)
                .HasDatabaseName("ix_terms_university_id");

            builder.HasIndex(x => x.Status)
                .HasDatabaseName("ix_terms_status");

            builder.HasIndex(x => new { x.StartDate, x.EndDate })
                .HasDatabaseName("ix_terms_dates");

            // Relationships
            builder.HasOne(x => x.University)
                .WithMany(u => u.Terms)
                .HasForeignKey(x => x.UniversityId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Internships)
                .WithOne(i => i.Term)
                .HasForeignKey(i => i.TermId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

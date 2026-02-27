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
    public class InternshipGroupConfiguration : IEntityTypeConfiguration<InternshipGroup>
    {
        public void Configure(EntityTypeBuilder<InternshipGroup> builder)
        {
            // Table name (snake_case + plural)
            builder.ToTable("internship_groups");

            // Primary key (BaseEntity)
            builder.HasKey(e => e.InternshipId);

            // ===== Properties =====

            builder.Property(e => e.TermId)
                .IsRequired()
                .HasColumnName("term_id");

            builder.Property(e => e.EnterpriseId)
                .HasColumnName("enterprise_id");

            builder.Property(e => e.MentorId)
                .IsRequired()
                .HasColumnName("mentor_id");

            builder.Property(e => e.StartDate)
                .HasColumnName("start_date");

            builder.Property(e => e.EndDate)
                .HasColumnName("end_date");

            // Enum conversion (theo guide dùng short)
            builder.Property(e => e.Status)
                .HasConversion<short>()
                .IsRequired()
                .HasColumnName("status");

            // ===== Indexes (FK nên có index) =====
            builder.HasIndex(e => e.InternshipId);
            builder.HasIndex(e => e.TermId);
            builder.HasIndex(e => e.EnterpriseId);
            builder.HasIndex(e => e.MentorId);

            // ===== Audit columns =====
            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            // ===== Relationship =====
            builder.HasOne(e => e.InternshipStudents)
                .WithOne()
                .HasForeignKey<InternshipStudents>(x => x.InternshipId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}

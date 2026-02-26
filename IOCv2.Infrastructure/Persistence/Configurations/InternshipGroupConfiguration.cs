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
            builder.ToTable("internship_groups");

            builder.HasKey(ig => ig.InternshipId);
            builder.Property(ig => ig.InternshipId)
                .HasColumnName("internship_id")
                .IsRequired();

            builder.Property(ig => ig.TermId).HasColumnName("term_id");
            
            builder.Property(ig => ig.EnterpriseId).HasColumnName("enterprise_id");

            builder.Property(ig => ig.MentorId).HasColumnName("mentor_id").IsRequired();

            builder.Property(ig => ig.StartDate).HasColumnName("start_date");

            builder.Property(ig => ig.EndDate).HasColumnName("end_date");

            builder.Property(ig => ig.Status).HasColumnName("status");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(ig => ig.Enterprise)
                .WithMany(e => e.InternshipGroups)
                .HasForeignKey(ig => ig.EnterpriseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(ig => ig.Mentor)
                .WithMany(m => m.InternshipGroups)
                .HasForeignKey(ig => ig.MentorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

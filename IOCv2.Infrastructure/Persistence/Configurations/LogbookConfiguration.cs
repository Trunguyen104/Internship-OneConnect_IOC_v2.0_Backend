using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class LogbookConfiguration : IEntityTypeConfiguration<Logbook>
    {
        public void Configure(EntityTypeBuilder<Logbook> builder)
        {
            builder.ToTable("logbooks");
            builder.HasKey(lb => lb.LogbookId);
            builder.Property(lb => lb.LogbookId).HasColumnName("logbook_id");

            builder.Property(lb => lb.InternshipId)
                .IsRequired()
                .HasColumnName("internship_id");

            builder.Property(lb => lb.StudentId)
                .IsRequired()
                .HasColumnName("student_id");

            builder.Property(lb => lb.Summary)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("summary");

            builder.Property(lb => lb.Issue)
                .HasMaxLength(200)
                .HasColumnName("issue");

            builder.Property(lb => lb.Plan)
                .HasMaxLength(200)
                .IsRequired()
                .HasColumnName("plan");

            builder.Property(lb => lb.DateReport)
                .IsRequired()
                .HasColumnName("date_report");

            builder.Property(lb => lb.Status).HasColumnName("status");

            builder.Property(e => e.CreatedAt).HasColumnName("created_at");
            builder.Property(e => e.CreatedBy).HasColumnName("created_by");
            builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            builder.Property(e => e.UpdatedBy).HasColumnName("updated_by");
            builder.Property(e => e.DeletedAt).HasColumnName("deleted_at");

            builder.HasOne(lb => lb.Student)
                .WithMany(s => s.Logbooks)
                .HasForeignKey(lb => lb.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(lb => lb.WorkItem)
                .WithMany(wi => wi.Logbook)
                .UsingEntity(j => j.ToTable("logbook_work_items"));

            builder.HasOne(lb => lb.InternshipGroup)
                .WithMany(i => i.Logbooks)
                .HasForeignKey(lb => lb.InternshipId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

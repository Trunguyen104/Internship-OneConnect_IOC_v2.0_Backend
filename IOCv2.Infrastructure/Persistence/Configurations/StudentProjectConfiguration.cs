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
    public class StudentProjectConfiguration : IEntityTypeConfiguration<StudentProject>
    {
        public void Configure(EntityTypeBuilder<StudentProject> builder)
        {
            builder.ToTable("student_project");

            // Composite Key
            builder.HasKey(sp => new { sp.StudentId, sp.ProjectId });

            builder.HasOne(sp => sp.Student)
                .WithMany(s => s.StudentProjects)
                .HasForeignKey(sp => sp.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sp => sp.Project)
                .WithMany(p => p.StudentProjects)
                .HasForeignKey(sp => sp.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index
            builder.HasIndex(sp => sp.StudentId);
            builder.HasIndex(sp => sp.ProjectId);
        }
    }
}

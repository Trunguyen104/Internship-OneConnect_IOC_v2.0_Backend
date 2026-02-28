using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class StudentTermConfiguration : IEntityTypeConfiguration<StudentTerm>
{
    public void Configure(EntityTypeBuilder<StudentTerm> builder)
    {
        builder.ToTable("student_terms");

        builder.HasKey(x => new { x.StudentId, x.TermId });

        builder.Property(x => x.StudentId).HasColumnName("student_id");
        builder.Property(x => x.TermId).HasColumnName("term_id");
        builder.Property(x => x.Status).HasColumnName("status").HasColumnType("smallint");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by");

        builder.HasOne(x => x.Student)
            .WithMany(s => s.StudentTerms)
            .HasForeignKey(x => x.StudentId);

        builder.HasOne(x => x.Term)
            .WithMany(t => t.StudentTerms)
            .HasForeignKey(x => x.TermId);
    }
}

using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class StakeholderIssueConfiguration : IEntityTypeConfiguration<StakeholderIssue>
    {
        public void Configure(EntityTypeBuilder<StakeholderIssue> builder)
        {
            builder.ToTable("stakeholder_issues");

            builder.HasKey(si => si.Id);

            builder.Property(si => si.Id)
                .HasColumnName("id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(si => si.StakeholderId)
                .HasColumnName("stakeholder_id")
                .IsRequired();

            builder.Property(si => si.Title)
                .HasColumnName("title")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(si => si.Description)
                .HasColumnName("description")
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(si => si.Status)
                .HasColumnName("status")
                .HasConversion<int>()
                .IsRequired();

            builder.Property(si => si.ResolvedAt)
                .HasColumnName("resolved_at");

            builder.Property(si => si.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("now()");

            builder.Property(si => si.CreatedBy)
                .HasColumnName("created_by");
                // Note: CreatedBy is Guid? — uuid column, HasMaxLength not applicable

            builder.Property(si => si.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(si => si.UpdatedBy)
                .HasColumnName("updated_by");
                // Note: UpdatedBy is Guid? — uuid column, HasMaxLength not applicable

            builder.Property(si => si.DeletedAt)
                .HasColumnName("deleted_at");

            builder.HasIndex(si => si.StakeholderId)
                .HasDatabaseName("ix_stakeholder_issues_stakeholder_id");

            builder.HasIndex(si => si.Status)
                .HasDatabaseName("ix_stakeholder_issues_status");

            builder.HasOne(si => si.Stakeholder)
                .WithMany(s => s.Issues)
                .HasForeignKey(si => si.StakeholderId)
                .HasConstraintName("fk_stakeholder_issues_stakeholders")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}


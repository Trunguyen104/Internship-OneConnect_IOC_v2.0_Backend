using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations;

public class GroupMentorHistoryConfiguration : IEntityTypeConfiguration<GroupMentorHistory>
{
    public void Configure(EntityTypeBuilder<GroupMentorHistory> builder)
    {
        builder.ToTable("group_mentor_history");

        builder.HasKey(h => h.HistoryId);
        builder.Property(h => h.HistoryId).HasColumnName("history_id");
        builder.Property(h => h.InternshipGroupId).HasColumnName("internship_group_id").IsRequired();
        builder.Property(h => h.OldMentorId).HasColumnName("old_mentor_id");
        builder.Property(h => h.NewMentorId).HasColumnName("new_mentor_id");
        builder.Property(h => h.ActorId).HasColumnName("actor_id").IsRequired();
        builder.Property(h => h.ActionType).HasColumnName("action_type").HasConversion<short>().IsRequired();
        builder.Property(h => h.Timestamp).HasColumnName("timestamp").IsRequired();

        builder.HasOne(h => h.InternshipGroup)
            .WithMany()
            .HasForeignKey(h => h.InternshipGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.OldMentor)
            .WithMany()
            .HasForeignKey(h => h.OldMentorId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(h => h.NewMentor)
            .WithMany()
            .HasForeignKey(h => h.NewMentorId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.InternshipGroupId)
            .HasDatabaseName("ix_group_mentor_history_group_id");

        builder.HasIndex(h => h.Timestamp)
            .HasDatabaseName("ix_group_mentor_history_timestamp");
    }
}

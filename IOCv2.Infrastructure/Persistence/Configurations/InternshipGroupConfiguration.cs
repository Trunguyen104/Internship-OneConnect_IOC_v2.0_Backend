using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class InternshipGroupConfiguration : IEntityTypeConfiguration<InternshipGroup>
    {
        public void Configure(EntityTypeBuilder<InternshipGroup> builder)
        {
            builder.ToTable("internship_groups");

            builder.HasKey(e => e.InternshipId);

            builder.Property(e => e.GroupName)
                .HasMaxLength(255)
                .IsRequired();

            // Mentor mapping
            builder.HasOne(e => e.Mentor)
                .WithMany(m => m.MentoringGroups)
                .HasForeignKey(e => e.MentorId)
                .OnDelete(DeleteBehavior.SetNull);

            // Enterprise mapping
            builder.HasOne(e => e.Enterprise)
                .WithMany(ent => ent.InternshipGroups)
                .HasForeignKey(e => e.EnterpriseId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}

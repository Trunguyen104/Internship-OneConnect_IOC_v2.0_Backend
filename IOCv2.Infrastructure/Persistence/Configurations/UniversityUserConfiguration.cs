using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    public class UniversityUserConfiguration : IEntityTypeConfiguration<UniversityUser>
    {
        public void Configure(EntityTypeBuilder<UniversityUser> builder)
        {
            builder.ToTable("university_users");
            builder.HasKey(uu => uu.UniversityUserId);
            builder.Property(uu => uu.UniversityUserId).HasColumnName("uni_user_id");

            builder.Property(uu => uu.UniversityId).HasColumnName("uni_id").IsRequired();
            builder.Property(uu => uu.UserId).HasColumnName("user_id").IsRequired();

            builder.HasIndex(uu => uu.UniversityId);
            builder.HasIndex(uu => uu.UserId).IsUnique();

            builder.Property(uu => uu.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
            builder.Property(uu => uu.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
            builder.Property(uu => uu.DeletedAt).HasColumnName("deleted_at").HasColumnType("timestamptz");
            builder.Property(uu => uu.CreatedBy).HasColumnName("created_by");
            builder.Property(uu => uu.UpdatedBy).HasColumnName("updated_by");

            builder.HasOne(uu => uu.University)
                .WithMany(u => u.UniversityUsers)
                .HasForeignKey(uu => uu.UniversityId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasOne(uu => uu.User)
                .WithOne(u => u.UniversityUser)
                .HasForeignKey<UniversityUser>(uu => uu.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);
        }
    }
}

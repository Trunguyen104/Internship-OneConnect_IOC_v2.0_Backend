using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOCv2.Infrastructure.Persistence.Configurations
{
    internal class UserCodeSequenceConfiguration
      : IEntityTypeConfiguration<UserCodeSequence>
    {
        public void Configure(EntityTypeBuilder<UserCodeSequence> builder)
        {
            builder.ToTable("user_code_sequences");

            builder.HasKey(x => x.Role);

            builder.Property(x => x.Role)
                .HasColumnName("role")
                .HasConversion<short>()
                .HasColumnType("smallint");

            builder.Property(x => x.CurrentNumber)
                .HasColumnName("current_number")
                .IsRequired();
        }
    }
}

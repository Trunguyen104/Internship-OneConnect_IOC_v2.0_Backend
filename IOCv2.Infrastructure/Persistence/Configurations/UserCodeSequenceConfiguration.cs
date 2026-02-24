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
            builder.HasKey(x => x.Role);

            builder.Property(x => x.CurrentNumber)
                .IsRequired();
        }
    }
}

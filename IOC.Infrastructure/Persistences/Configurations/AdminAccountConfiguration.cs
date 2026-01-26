using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IOC.Infrastructure.Persistences.Configurations
{
    

    public class AdminAccountConfiguration
        : IEntityTypeConfiguration<AdminAccount>
    {
        public void Configure(EntityTypeBuilder<AdminAccount> builder)
        {
            builder.ToTable("admin_accounts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedNever();

            builder.Property(x => x.Code)
                .HasMaxLength(50);

            builder.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.Email)
                .HasConversion(
                    v => v.Value,
                    v => Domain.ValueObjects.Email.Create(v))
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.Property(x => x.Role)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.OrganizationId)
                .IsRequired(false);

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Organization)
                .WithMany()
                .HasForeignKey(x => x.OrganizationId);

        }
    }
}

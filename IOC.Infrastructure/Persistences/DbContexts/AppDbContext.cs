using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOC.Domain.Entities;
using IOC.Infrastructure.Persistences.Configurations;
using Microsoft.EntityFrameworkCore;

namespace IOC.Infrastructure.Persistences.DbContexts
{


    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AdminAccount> AdminAccounts => Set<AdminAccount>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<Organization> Organizations => Set<Organization>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new AdminAccountConfiguration());
            modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
            modelBuilder.ApplyConfiguration(new OrganizationConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }

}

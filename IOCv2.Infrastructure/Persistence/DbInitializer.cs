using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

using static IOCv2.Application.Constants.MessageKeys;

namespace IOCv2.Infrastructure.Persistence
{
    public class DbInitializer
    {
        private readonly AppDbContext _context;
        private readonly IPasswordService _passwordService;

        public DbInitializer(AppDbContext context, IPasswordService passwordService)
        {
            _context = context;
            _passwordService = passwordService;
        }

        public void Initialize()
        {
            // Auto Migrate
       
            if (_context.Database.GetPendingMigrations().Any())
            {
                _context.Database.Migrate();
            }

            // Seed Data
            if (_context.Users.Any())
            {
                return;
            }

            //var users = new User[]
            //{
            //    new User
            //    {
            //        Id = Guid.NewGuid(),
            //        Username = "admin",
            //        PasswordHash = _passwordService.HashPassword("admin"),
            //        FullName = "Admin Manager",
            //        Email = "admin@foodhub.com",
            //        PhoneNumber = "0909000001",
            //        Role = EmployeeRole.Manager,
            //        Status = EmployeeStatus.Active,
            //        CreatedAt = DateTime.UtcNow
            //    },
            //    new Employee
            //    {
            //        EmployeeId = Guid.NewGuid(),
            //        EmployeeCode = "B002001",
            //        Username = "chef",
            //        PasswordHash = _passwordService.HashPassword("chef"),
            //        FullName = "Chief Chef",
            //        Email = "chef@foodhub.com",
            //        Phone = "0909000002",
            //        Role = EmployeeRole.ChefBar,
            //        Status = EmployeeStatus.Active,
            //        CreatedAt = DateTime.UtcNow
            //    },
            //    new Employee
            //    {
            //        EmployeeId = Guid.NewGuid(),
            //        EmployeeCode = "W003001",
            //        Username = "waiter",
            //        PasswordHash = _passwordService.HashPassword("waiter"),
            //        FullName = "Waiter One",
            //        Email = "waiter@foodhub.com",
            //        Phone = "0909000003",
            //        Role = EmployeeRole.Waiter,
            //        Status = EmployeeStatus.Active,
            //        CreatedAt = DateTime.UtcNow
            //    },
            //    new Employee
            //    {
            //        EmployeeId = Guid.NewGuid(),
            //        EmployeeCode = "C004001",
            //        Username = "cashier",
            //        PasswordHash = _passwordService.HashPassword("cashier"),
            //        FullName = "Cashier One",
            //        Email = "cashier@foodhub.com",
            //        Phone = "0909000004",
            //        Role = EmployeeRole.Cashier,
            //        Status = EmployeeStatus.Active,
            //        CreatedAt = DateTime.UtcNow
            //    }
            //};

            //foreach (var e in employees)
            //{
            //    // Check if already exists to avoid duplicate key errors
            //    if (!_context.Employees.Any(x => x.EmployeeCode == e.EmployeeCode || x.Username == e.Username || x.Email == e.Email))
            //    {
            //        _context.Employees.Add(e);

            //        // Add Audit Log for Seed Data
            //        _context.AuditLogs.Add(new AuditLog
            //        {
            //            LogId = Guid.NewGuid(),
            //            Action = AuditAction.Create,
            //            TargetId = e.EmployeeId,
            //            PerformedByEmployeeId = e.EmployeeId, // Self-created for seed
            //            CreatedAt = DateTimeOffset.UtcNow,
            //            Reason = "Seed data initialization",
            //            Metadata = "{\"info\": \"System generated\"}" // Valid JSON for jsonb column
            //        });
            //    }
            //}
            _context.SaveChanges();
        }
    }
}

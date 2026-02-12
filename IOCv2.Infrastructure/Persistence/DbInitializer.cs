using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

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

            // Seed Data
            if (_context.Users.Any())
            {
                return;
            }

            var adminUser = new User
            {
                UserId = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = _passwordService.HashPassword("admin123"),
                FullName = "System Administrator",
                Email = "admin@iocv2.com",
                Role = UserRole.SuperAdmin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
            _context.SaveChanges();
        }
    }
}

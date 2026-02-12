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
            // Kiểm tra và tạo Super Admin
            if (!_context.Users.Any(u => u.Role == UserRole.SuperAdmin))
            {
                var superAdmin = new User
                {
                    UserId = Guid.NewGuid(),
                    UserCode = "SU0001",
                    Username = "admin",
                    PasswordHash = _passwordService.HashPassword("Admin@123"), // Mật khẩu mạnh hơn
                    FullName = "Super Administrator",
                    Email = "admin@iocv2.com",
                    Role = UserRole.SuperAdmin,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(superAdmin);
            }

            // Lưu thay đổi (nếu có)
            if (_context.ChangeTracker.HasChanges())
            {
                _context.SaveChanges();
            }
        }
    }
}

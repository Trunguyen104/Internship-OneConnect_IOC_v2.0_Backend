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
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    PasswordHash = _passwordService.HashPassword("Admin@123"), // Mật khẩu mạnh hơn
                    FullName = "Super Administrator",
                    Email = "admin@iocv2.com",
                    Role = UserRole.SuperAdmin,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(adminUser);
            }
            // Kiểm tra và tạo School Admin (Đại học)
            if (!_context.Users.Any(u => u.Role == UserRole.SchoolAdmin))
            {
                var schoolUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "fpt_admin",
                    PasswordHash = _passwordService.HashPassword("School@123"),
                    FullName = "FPT University Admin",
                    Email = "admin@fpt.edu.vn",
                    Role = UserRole.SchoolAdmin,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(schoolUser);
            }
            // Kiểm tra và tạo Enterprise Admin (Doanh nghiệp)
            if (!_context.Users.Any(u => u.Role == UserRole.EnterpriseAdmin))
            {
                var enterpriseUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "rikkei_admin",
                    PasswordHash = _passwordService.HashPassword("Enterprise@123"),
                    FullName = "RikkeiSoft HR",
                    Email = "hr@rikkeisoft.com",
                    Role = UserRole.EnterpriseAdmin, // Hoặc EnterpriseAdmin
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(enterpriseUser);
            }

            // Kiểm tra và tạo Student
            if (!_context.Users.Any(u => u.Role == UserRole.Student))
            {
                var studentUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "trunguyen",
                    PasswordHash = _passwordService.HashPassword("Tn@123456"),
                    FullName = "Trung Nguyen",
                    Email = "nguyennt.ce191135@gmail.com",
                    Role = UserRole.Student,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Users.Add(studentUser);
            }
            // Lưu thay đổi (nếu có)
            if (_context.ChangeTracker.HasChanges())
            {
                _context.SaveChanges();
            }
        }
    }
}

using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;

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

        public async Task InitializeAsync()
        {
            await SeedUniversities();
            await SeedEnterprises();
            await SeedUsers();

            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedUniversities()
        {
            if (!await _context.Universities.AnyAsync())
            {
                var universities = new List<University>
                {
                    new University
                    {
                        UniversityId = Guid.NewGuid(),
                        Code = "FPTU",
                        Name = "FPT University",
                        Address = "Hoa Lac Hi-Tech Park, Hanoi",
                        Status = 1
                    },
                    new University
                    {
                        UniversityId = Guid.NewGuid(),
                        Code = "NEU",
                        Name = "National Economics University",
                        Address = "207 Giai Phong, Dong Da, Hanoi",
                        Status = 1
                    }
                };
                await _context.Universities.AddRangeAsync(universities);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedEnterprises()
        {
            if (!await _context.Enterprises.AnyAsync())
            {
                var enterprises = new List<Enterprise>
                {
                    new Enterprise
                    {
                        EnterpriseId = Guid.NewGuid(),
                        Name = "FPT Software",
                        TaxCode = "0101248141",
                        Industry = "Information Technology",
                        Address = "Hoa Lac Hi-Tech Park, Hanoi",
                        IsVerified = true,
                        Status = 1
                    },
                    new Enterprise
                    {
                        EnterpriseId = Guid.NewGuid(),
                        Name = "Viettel Group",
                        TaxCode = "0100109106",
                        Industry = "Telecommunications",
                        Address = "Giang Vo, Ba Dinh, Hanoi",
                        IsVerified = true,
                        Status = 1
                    }
                };
                await _context.Enterprises.AddRangeAsync(enterprises);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedUsers()
        {
            var passHash = _passwordService.HashPassword("Admin@123");
            var universityList = await _context.Universities.ToListAsync();
            var enterpriseList = await _context.Enterprises.ToListAsync();

            // 1. Super Admin
            if (!await _context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
            {
                var superAdmin = new User
                {
                    UserId = Guid.NewGuid(),
                    UserCode = "SU000001",
                    PasswordHash = passHash,
                    FullName = "Super Administrator",
                    Email = "admin@iocv2.com",
                    Role = UserRole.SuperAdmin,
                    Status = UserStatus.Active,
                    Gender = UserGender.Other
                };
                _context.Users.Add(superAdmin);
            }

            // 2. Moderator
            if (!await _context.Users.AnyAsync(u => u.Role == UserRole.Moderator))
            {
                var moderator = new User
                {
                    UserId = Guid.NewGuid(),
                    UserCode = "MO000001",
                    PasswordHash = passHash,
                    FullName = "System Moderator",
                    Email = "moderator@iocv2.com",
                    Role = UserRole.Moderator,
                    Status = UserStatus.Active,
                    Gender = UserGender.Male
                };
                _context.Users.Add(moderator);
            }

            // 3. School Admins (1 for each Uni)
            foreach (var uni in universityList)
            {
                var email = $"admin@{uni.Code.ToLower()}.edu.vn";
                if (!await _context.Users.AnyAsync(u => u.Email == email))
                {
                    var user = new User
                    {
                        UserId = Guid.NewGuid(),
                        UserCode = $"SC_{uni.Code}_01",
                        PasswordHash = passHash,
                        FullName = $"{uni.Code} Administrator",
                        Email = email,
                        Role = UserRole.SchoolAdmin,
                        Status = UserStatus.Active
                    };
                    _context.Users.Add(user);
                    _context.UniversityUsers.Add(new UniversityUser { UserId = user.UserId, UniversityId = uni.UniversityId });
                }
            }

            // 4. Enterprise Admins (1 for each Enterprise)
            foreach (var ent in enterpriseList)
            {
                var email = $"admin@{ent.Name.Replace(" ", "").ToLower()}.com";
                if (!await _context.Users.AnyAsync(u => u.Email == email))
                {
                    var user = new User
                    {
                        UserId = Guid.NewGuid(),
                        UserCode = $"EN_{ent.Name.Substring(0, 3).ToUpper()}_01",
                        PasswordHash = passHash,
                        FullName = $"{ent.Name} Admin",
                        Email = email,
                        Role = UserRole.EnterpriseAdmin,
                        Status = UserStatus.Active
                    };
                    _context.Users.Add(user);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { UserId = user.UserId, EnterpriseId = ent.EnterpriseId });
                }
            }

            // 5. HR & Mentor for FPT
            var fpt = enterpriseList.FirstOrDefault(e => e.Name.Contains("FPT"));
            if (fpt != null)
            {
                // HR
                if (!await _context.Users.AnyAsync(u => u.Email == "hr.fpt@iocv2.com"))
                {
                    var hr = new User
                    {
                        UserId = Guid.NewGuid(),
                        UserCode = "HR000001",
                        PasswordHash = passHash,
                        FullName = "FPT HR Manager",
                        Email = "hr.fpt@iocv2.com",
                        Role = UserRole.HR,
                        Status = UserStatus.Active
                    };
                    _context.Users.Add(hr);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { UserId = hr.UserId, EnterpriseId = fpt.EnterpriseId, Position = "HR Manager" });
                }

                // Mentor
                if (!await _context.Users.AnyAsync(u => u.Email == "mentor.fpt@iocv2.com"))
                {
                    var mentor = new User
                    {
                        UserId = Guid.NewGuid(),
                        UserCode = "ME000001",
                        PasswordHash = passHash,
                        FullName = "FPT Senior Mentor",
                        Email = "mentor.fpt@iocv2.com",
                        Role = UserRole.Mentor,
                        Status = UserStatus.Active
                    };
                    _context.Users.Add(mentor);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { UserId = mentor.UserId, EnterpriseId = fpt.EnterpriseId, Position = "Technical Lead" });
                }
            }

            // 6. Students (3 for FPTU, 2 for NEU)
            foreach (var uni in universityList)
            {
                int count = uni.Code == "FPTU" ? 3 : 2;
                for (int i = 1; i <= count; i++)
                {
                    var email = $"student{i}@{uni.Code.ToLower()}.edu.vn";
                    if (!await _context.Users.AnyAsync(u => u.Email == email))
                    {
                        var user = new User
                        {
                            UserId = Guid.NewGuid(),
                            UserCode = $"ST_{uni.Code}_{i:D3}",
                            PasswordHash = passHash,
                            FullName = $"Student {i} of {uni.Code}",
                            Email = email,
                            Role = UserRole.Student,
                            Status = UserStatus.Active
                        };
                        _context.Users.Add(user);
                        _context.UniversityUsers.Add(new UniversityUser { UserId = user.UserId, UniversityId = uni.UniversityId });
                        _context.Students.Add(new Student 
                        { 
                            StudentId = Guid.NewGuid(), 
                            UserId = user.UserId, 
                            Status = StudentStatus.NO_INTERNSHIP,
                            Major = uni.Code == "FPTU" ? "Computer Science" : "Business Administration",
                            Class = $"{uni.Code}_K{65 + i}"
                        });
                    }
                }
            }

            // 7. Test Student Account
            var fptu = universityList.FirstOrDefault(u => u.Code == "FPTU");
            if (fptu != null && !await _context.Users.AnyAsync(u => u.Email == "trunguyen.104@gmail.com"))
            {
                var testUser = new User
                {
                    UserId = Guid.NewGuid(),
                    UserCode = "ST_TRUNG_01",
                    PasswordHash = passHash,
                    FullName = "Trung Nguyen",
                    Email = "trunguyen.104@gmail.com",
                    Role = UserRole.Student,
                    Status = UserStatus.Active,
                    Gender = UserGender.Male
                };
                _context.Users.Add(testUser);
                _context.UniversityUsers.Add(new UniversityUser { UserId = testUser.UserId, UniversityId = fptu.UniversityId });
                _context.Students.Add(new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = testUser.UserId,
                    Status = StudentStatus.NO_INTERNSHIP,
                    Major = "Software Engineering",
                    Class = "SE1801"
                });
            }
        }
    }
}

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
            await SeedProjectsMockData();

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
                Console.WriteLine($"Seeded University IDs: {string.Join(", ", universities.Select(u => u.UniversityId))}");
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
            var existingEmails = await _context.Users.Select(u => u.Email).ToHashSetAsync();

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
                if (!existingEmails.Contains(email))
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
                if (!existingEmails.Contains(email))
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
                if (!existingEmails.Contains("hr.fpt@iocv2.com"))
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
                if (!existingEmails.Contains("mentor.fpt@iocv2.com"))
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
                    if (!existingEmails.Contains(email))
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
            if (fptu != null && !existingEmails.Contains("trunguyen.104@gmail.com"))
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

            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedProjectsMockData()
        {
            if (!await _context.Projects.AnyAsync())
            {
                var fptu = await _context.Universities.FirstOrDefaultAsync(u => u.Code == "FPTU");
                var fptSoft = await _context.Enterprises.FirstOrDefaultAsync(e => e.Name.Contains("FPT"));
                var mainStudent = await _context.Users.Include(u => u.Student).FirstOrDefaultAsync(u => u.Email == "trunguyen.104@gmail.com");
                var mentor = await _context.EnterpriseUsers.FirstOrDefaultAsync(eu => eu.User.Email == "mentor.fpt@iocv2.com");

                if (fptu != null && fptSoft != null && mainStudent?.Student != null && mentor != null)
                {
                    // 1. Tạo Term
                    var term = new Term
                    {
                        TermId = Guid.NewGuid(),
                        UniversityId = fptu.UniversityId,
                        Name = "Spring 2026",
                        StartDate = DateTime.UtcNow.AddMonths(-1),
                        EndDate = DateTime.UtcNow.AddMonths(3),
                        Status = 1
                    };
                    _context.Terms.Add(term);

                    // 2. Tạo Job
                    var job = new Job
                    {
                        JobId = Guid.NewGuid(),
                        EnterpriseId = fptSoft.EnterpriseId,
                        Title = "Backend .NET Developer Intern",
                        Description = "Join FPT Software as a backend intern building enterprise apps.",
                        Location = "Hoa Lac Hi-Tech Park",
                        InternshipDuration = 3,
                        Status = JobStatus.OPEN
                    };
                    _context.Jobs.Add(job);

                    // 3. Tạo Internship (hồ sơ thực tập của sinh viên Trung Nguyen)
                    var internship = new Internship
                    {
                        InternshipId = Guid.NewGuid(),
                        TermId = term.TermId,
                        StudentId = mainStudent.Student.StudentId,
                        JobId = job.JobId,
                        MentorId = mentor.EnterpriseUserId,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(3),
                        Status = InternshipStatus.IN_PROGRESS
                    };
                    _context.Internships.Add(internship);

                    // 4. Tạo Project mẫu
                    var project = new Project
                    {
                        ProjectId = Guid.NewGuid(),
                        InternshipId = internship.InternshipId,
                        MentorId = mentor.EnterpriseUserId,
                        ProjectName = "Internship OneConnect v2.0",
                        Field = "Web Application (Backend/.NET)",
                        Description = "Hệ thống quản lý thực tập sinh version 2.0 dành cho Đại học và Doanh nghiệp.",
                        Tags = "csharp, dotnet, postgresql",
                        ViewCount = 104,
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddMonths(2),
                        Status = ProjectStatus.IN_PROGRESS
                    };
                    _context.Projects.Add(project);

                    // 5. Thêm Members vào Project
                    var leaderMember = new ProjectMember
                    {
                        ProjectMemberId = Guid.NewGuid(),
                        ProjectId = project.ProjectId,
                        StudentId = mainStudent.Student.StudentId, // Trung
                        Role = ProjectMemberRole.LEADER,
                        Status = MemberStatus.ACTIVE
                    };
                    _context.ProjectMembers.Add(leaderMember);

                    // Thêm 1 sinh viên nữa vào nhóm (Student 1 FPTU)
                    var stu1 = await _context.Users.Include(u => u.Student).FirstOrDefaultAsync(u => u.Email == "student1@fptu.edu.vn");
                    if (stu1?.Student != null)
                    {
                        var normalMember = new ProjectMember
                        {
                            ProjectMemberId = Guid.NewGuid(),
                            ProjectId = project.ProjectId,
                            StudentId = stu1.Student.StudentId,
                            Role = ProjectMemberRole.MEMBER,
                            Status = MemberStatus.ACTIVE
                        };
                        _context.ProjectMembers.Add(normalMember);
                    }

                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}

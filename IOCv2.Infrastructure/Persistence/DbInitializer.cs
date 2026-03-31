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
        private readonly IUserServices _userService;

        private static class SeedIds
        {
            public static readonly Guid SuperAdminId = new Guid("00000000-0000-0000-0000-000000000001");
            public static readonly Guid FptuId = new Guid("11111111-1111-1111-1111-111111111111");
            public static readonly Guid FptuCtId = new Guid("11111111-1111-1111-1111-111111111112");
            public static readonly Guid FptSoftwareId = new Guid("22222222-2222-2222-2222-222222222201");
            public static readonly Guid RikkeisoftId = new Guid("22222222-2222-2222-2222-222222222202");

            public static readonly Guid SchoolAdminFptId = new Guid("33333333-3333-3333-3333-333333330001");
            public static readonly Guid EntAdminFptId = new Guid("44444444-4444-4444-4444-444444440001");
            public static readonly Guid MentorFptId = new Guid("55555555-5555-5555-5555-555555550001");
            public static readonly Guid SchoolAdminFptCtId = new Guid("33333333-3333-3333-3333-333333330002");
            public static readonly Guid EntAdminRikkeisoftId = new Guid("44444444-4444-4444-4444-444444440002");
            public static readonly Guid MentorRikkeisoftId = new Guid("55555555-5555-5555-5555-555555550002");
            public static readonly Guid MentorFptAltId = new Guid("55555555-5555-5555-5555-555555550011");
            public static readonly Guid MentorRikkeisoftAltId = new Guid("55555555-5555-5555-5555-555555550012");

            // Added HR seed ids for deterministic seeding
            public static readonly Guid HrFptId = new Guid("77777777-7777-7777-7777-777777770001");
            public static readonly Guid HrRikkeisoftId = new Guid("77777777-7777-7777-7777-777777770002");

            // Deterministic EnterpriseUser IDs for mentors — phải cố định để group.MentorId khớp sau mỗi lần seed
            // mentor@fptsoftware.com  → dùng EnterpriseUserId này khi tạo project cho FPT groups
            // mentor@rikkeisoft.com   → dùng EnterpriseUserId này khi tạo project cho Rikkeisoft groups
            public static readonly Guid MentorFptEuId = new Guid("88888888-8888-8888-8888-888888880001");
            public static readonly Guid MentorRikkeisoftEuId = new Guid("88888888-8888-8888-8888-888888880002");
            public static readonly Guid MentorFptAltEuId = new Guid("88888888-8888-8888-8888-888888880011");
            public static readonly Guid MentorRikkeisoftAltEuId = new Guid("88888888-8888-8888-8888-888888880012");

            public static readonly List<Guid> StudentIds = new()
            {
                new Guid("66666666-6666-6666-6666-666666660001"),
                new Guid("66666666-6666-6666-6666-666666660002"),
                new Guid("66666666-6666-6666-6666-666666660003"),
                new Guid("66666666-6666-6666-6666-666666660004"),
                new Guid("66666666-6666-6666-6666-666666660005")
            };

            // Deterministic id for the sixth seeded student (used for job-apply tests)
            public static readonly Guid Student6UserId = new Guid("66666666-6666-6666-6666-666666660006");

            // Deterministic IDs for UniAdmin monitoring test scenario students (s11–s16)
            // s11=NoGroup, s12=PendingConfirmation, s13=Unplaced, s14=Completed, s15=100%Logbook, s16=NoMentor
            public static readonly Guid Student11UserId = new Guid("66666666-6666-6666-6666-666666660011");
            public static readonly Guid Student12UserId = new Guid("66666666-6666-6666-6666-666666660012");
            public static readonly Guid Student13UserId = new Guid("66666666-6666-6666-6666-666666660013");
            public static readonly Guid Student14UserId = new Guid("66666666-6666-6666-6666-666666660014");
            public static readonly Guid Student15UserId = new Guid("66666666-6666-6666-6666-666666660015");
            public static readonly Guid Student16UserId = new Guid("66666666-6666-6666-6666-666666660016");
        }

        public DbInitializer(AppDbContext context, IPasswordService passwordService, IUserServices userService)
        {
            _context = context;
            _passwordService = passwordService;
            _userService = userService;
        }

        public async Task InitializeAsync()
        {
            await SeedUniversities();
            await SeedEnterprises();
            await SeedJobs(); // added: seed test jobs for enterprises
            await SeedUsers();
            await SeedTerms();
            await SeedInternshipPhases();   // ← must be before SeedInternshipGroups
            await SeedInternshipGroups();
            await SeedProjectsAndWorkItems();
            await SeedManageIGProjectData();
            await SeedInternshipStudents();
            await SeedUniAdminTestScenarios();
            await SeedLogbooks();
            await SeedStakeholdersAndIssues();
            await SeedProjectResources();
            await SeedViolationReports();
            await SeedEvaluations();
            await SeedNotifications();
            await SeedSprintWorkItems();
            await SeedAuditLogs();
            await SeedSecurityTokens();

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
                    University.Create("FPTU", "FPT University", "Hoa Lac Hi-Tech Park, Hanoi", null),
                    University.Create("FPTU-CT", "FPT University Can Tho", "600 Nguyen Van Cu, Ninh Kieu, Can Tho", null)
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
                        EnterpriseId = SeedIds.FptSoftwareId,
                        Name = "FPT Software",
                        TaxCode = "0102100740",
                        Industry = "Công nghệ thông tin",
                        Address = "Khu Công nghệ cao Hòa Lạc, Thạch Thất, Hà Nội",
                        Website = "https://www.fpt-software.com",
                        Description = "Tập đoàn công nghệ hàng đầu Việt Nam",
                        Status = (short)EnterpriseStatus.Active
                    },
                    new Enterprise
                    {
                        EnterpriseId = SeedIds.RikkeisoftId,
                        Name = "Rikkeisoft",
                        TaxCode = "0105844895",
                        Industry = "Phát triển phần mềm",
                        Address = "Tầng 21, Tòa nhà Handico, Phạm Hùng, Nam Từ Liêm, Hà Nội",
                        Website = "https://rikkeisoft.com",
                        Description = "Đối tác tin cậy về chuyển đổi số",
                        Status = (short)EnterpriseStatus.Active
                    }
                };
                await _context.Enterprises.AddRangeAsync(enterprises);
                await _context.SaveChangesAsync();
            }
        }

        // New: seed a couple of test jobs tied to seeded enterprises (use EF entities)
        private async Task SeedJobs()
        {
            if (await _context.Jobs.AnyAsync()) return;

            // Ensure expire dates are comfortably in the future and Position is set (DB requires it)
            var job1 = Job.Create(
                SeedIds.FptSoftwareId,
                "Junior .NET Intern",
                "Assist backend team building APIs for the IOC v2 platform.",
                "C#, .NET, EF Core, REST, basic SQL",
                "Monthly stipend, mentorship, certificate",
                "Hà Nội (Hybrid)",
                2,
                DateTime.UtcNow.AddMonths(2)
            );
            job1.Position = "Backend Intern";
            job1.Status = JobStatus.PUBLISHED;

            var job2 = Job.Create(
                SeedIds.RikkeisoftId,
                "Frontend Intern (Angular)",
                "Work on feature improvements and UI polishing for legacy CRM.",
                "Angular, TypeScript, HTML/CSS, basic RxJS",
                "Stipend, mentorship, certificate",
                "Hà Nội (On-site)",
                1,
                DateTime.UtcNow.AddMonths(2)
            );
            job2.Position = "Frontend Intern";
            job2.Status = JobStatus.PUBLISHED;

            await _context.Jobs.AddRangeAsync(job1, job2);
            await _context.SaveChangesAsync();
        }

        private async Task SeedUsers()
        {
            var passHash = _passwordService.HashPassword("Admin@123");
            var universityList = await _context.Universities.ToListAsync();
            var enterpriseList = await _context.Enterprises.ToListAsync();
            var existingEmails = await _context.Users
                .IgnoreQueryFilters()
                .Select(u => u.Email)
                .ToHashSetAsync();
            CancellationToken cancellationToken = default;
            int phoneCounter = 1000;

            // 1. Super Admin
            if (!await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.SuperAdmin))
            {
                var userId = SeedIds.SuperAdminId;
                var userCode = await _userService.GenerateUserCodeAsync(UserRole.SuperAdmin, cancellationToken);
                var superAdmin = new User(userId, userCode, "admin@iocv2.com", "Super Administrator", UserRole.SuperAdmin, passHash);
                superAdmin.UpdateProfile(superAdmin.FullName, $"098765{phoneCounter++}", null, UserGender.Male, new DateOnly(1980, 1, 1), "Hà Nội");
                superAdmin.SetStatus(UserStatus.Active);
                _context.Users.Add(superAdmin);
                existingEmails.Add(superAdmin.Email);
            }

            // 2. Enterprise Admins, HRs & Mentors
            foreach (var ent in enterpriseList)
            {
                Guid adminId, mentorId, hrId;
                if (ent.EnterpriseId == SeedIds.FptSoftwareId)
                {
                    adminId = SeedIds.EntAdminFptId;
                    mentorId = SeedIds.MentorFptId;
                    hrId = SeedIds.HrFptId;
                }
                else if (ent.EnterpriseId == SeedIds.RikkeisoftId)
                {
                    adminId = SeedIds.EntAdminRikkeisoftId;
                    mentorId = SeedIds.MentorRikkeisoftId;
                    hrId = SeedIds.HrRikkeisoftId;
                }
                else
                {
                    adminId = Guid.NewGuid();
                    mentorId = Guid.NewGuid();
                    hrId = Guid.NewGuid();
                }

                var baseName = ent.Name.Replace(" ", "").ToLower();

                var adminEmail = $"admin@{baseName}.com";
                if (!existingEmails.Contains(adminEmail))
                {
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.EnterpriseAdmin, cancellationToken);
                    var user = new User(adminId, userCode, adminEmail, $"Admin of {ent.Name}", UserRole.EnterpriseAdmin, passHash);
                    user.UpdateProfile(user.FullName, $"098765{phoneCounter++}", null, UserGender.Male, new DateOnly(1985, 1, 1), ent.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(adminEmail);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), UserId = user.UserId, EnterpriseId = ent.EnterpriseId, Position = "Enterprise Administrator" });
                }

                var mentorEmail = $"mentor@{baseName}.com";
                if (!existingEmails.Contains(mentorEmail))
                {
                    // Dùng deterministic EnterpriseUserId để group.MentorId khớp sau mọi lần seed lại DB
                    var mentorEuId = ent.EnterpriseId == SeedIds.FptSoftwareId
                        ? SeedIds.MentorFptEuId
                        : ent.EnterpriseId == SeedIds.RikkeisoftId
                            ? SeedIds.MentorRikkeisoftEuId
                            : Guid.NewGuid();

                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Mentor, cancellationToken);
                    var user = new User(mentorId, userCode, mentorEmail, $"Mentor {ent.Name}", UserRole.Mentor, passHash);
                    user.UpdateProfile(user.FullName, $"098765{phoneCounter++}", null, UserGender.Male, new DateOnly(1990, 1, 1), ent.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(mentorEmail);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { EnterpriseUserId = mentorEuId, UserId = user.UserId, EnterpriseId = ent.EnterpriseId, Position = "Technical Mentor" });
                }

                // Seed a secondary mentor in the same enterprise to support mentor-reassignment scenarios.
                var mentorAltEmail = $"mentor2@{baseName}.com";
                if (!existingEmails.Contains(mentorAltEmail))
                {
                    var mentorAltUserId = ent.EnterpriseId == SeedIds.FptSoftwareId
                        ? SeedIds.MentorFptAltId
                        : ent.EnterpriseId == SeedIds.RikkeisoftId
                            ? SeedIds.MentorRikkeisoftAltId
                            : Guid.NewGuid();

                    var mentorAltEuId = ent.EnterpriseId == SeedIds.FptSoftwareId
                        ? SeedIds.MentorFptAltEuId
                        : ent.EnterpriseId == SeedIds.RikkeisoftId
                            ? SeedIds.MentorRikkeisoftAltEuId
                            : Guid.NewGuid();

                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Mentor, cancellationToken);
                    var user = new User(mentorAltUserId, userCode, mentorAltEmail, $"Mentor 2 {ent.Name}", UserRole.Mentor, passHash);
                    user.UpdateProfile(user.FullName, $"098765{phoneCounter++}", null, UserGender.Male, new DateOnly(1991, 1, 1), ent.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(mentorAltEmail);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { EnterpriseUserId = mentorAltEuId, UserId = user.UserId, EnterpriseId = ent.EnterpriseId, Position = "Technical Mentor" });
                }

                // HR account (new)
                var hrEmail = $"hr@{baseName}.com";
                if (!existingEmails.Contains(hrEmail))
                {
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.HR, cancellationToken);
                    var user = new User(hrId, userCode, hrEmail, $"HR {ent.Name}", UserRole.HR, passHash);
                    user.UpdateProfile(user.FullName, $"098765{phoneCounter++}", null, UserGender.Female, new DateOnly(1992, 1, 1), ent.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(hrEmail);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), UserId = user.UserId, EnterpriseId = ent.EnterpriseId, Position = "HR" });
                }
            }
           
            // 3. School Admin accounts
            foreach (var uni in universityList)
            {
                Guid uniAdminId;
                if (uni.Code == "FPTU") uniAdminId = SeedIds.SchoolAdminFptId;
                else if (uni.Code == "FPTU-CT") uniAdminId = SeedIds.SchoolAdminFptCtId;
                else uniAdminId = Guid.NewGuid();

                var uniAdminEmail = $"schooladmin@{uni.Code.ToLower()}.com";
                if (!existingEmails.Contains(uniAdminEmail))
                {
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.SchoolAdmin, cancellationToken);
                    var user = new User(uniAdminId, userCode, uniAdminEmail, $"School Admin {uni.Code}", UserRole.SchoolAdmin, passHash);
                    user.UpdateProfile(user.FullName, $"098765{phoneCounter++}", null, UserGender.Male, new DateOnly(1985, 1, 1), uni.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(uniAdminEmail);
                    _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user.UserId, UniversityId = uni.UniversityId, Position = "School Administrator" });
                }
            }

            // 4. Seed 16 specific students (s1-10: original; s11-16: UniAdmin monitoring test scenarios)
            string[] studentEmails = {
                "student1@fptu.edu.vn",
                "student2@fptu.edu.vn",
                "student3@fptu.edu.vn",
                "student4@fptu.edu.vn",
                "student5@fptu.edu.vn",
                "student6@fptu.edu.vn",
                "student7@fptu.edu.vn",
                "student8@fptu.edu.vn",
                "student9@fptu.edu.vn",
                "student10@fptu.edu.vn",
                "student11@fptu.edu.vn",
                "student12@fptu.edu.vn",
                "student13@fptu.edu.vn",
                "student14@fptu.edu.vn",
                "student15@fptu.edu.vn",
                "student16@fptu.edu.vn"
            };

            string[] studentNames = {
                "Nguyễn Văn An",
                "Trần Thị Bình",
                "Lê Văn Cường",
                "Phạm Thị Dung",
                "Hoàng Văn Em",
                "Đỗ Thị Phương",
                "Vũ Văn Giang",
                "Bùi Thị Hoa",
                "Đinh Văn Nghĩa",
                "Ngô Thị Kiều",
                "Phạm Minh Khôi",          // s11 – NoGroup scenario
                "Vũ Thị Lan",               // s12 – PendingConfirmation scenario
                "Đặng Văn Mạnh",            // s13 – Unplaced scenario
                "Hoàng Thị Ngọc",           // s14 – Completed scenario
                "Bùi Văn Phong",            // s15 – 100% Logbook scenario
                "Lý Thị Quỳnh"             // s16 – NoMentor group scenario
            };

            string[] studentClasses = {
                "SE1616", "SE1617", "SE1616", "SE1618", "SE1617",
                "SE1619", "SE1616", "SE1618", "SE1619", "SE1617",
                "SE1620", "SE1620", "SE1621", "SE1621", "SE1620", "SE1621"
            };

            string[] studentMajors = {
                "Software Engineering", "Software Engineering", "Information Technology",
                "Software Engineering", "Information Technology", "Software Engineering",
                "Computer Science", "Information Technology", "Software Engineering", "Computer Science",
                "Software Engineering", "Information Technology", "Software Engineering",
                "Computer Science", "Information Technology", "Software Engineering"
            };

            for (int i = 0; i < studentEmails.Length; i++)
            {
                if (!existingEmails.Contains(studentEmails[i]))
                {
                    // Keep student6 deterministic for job-apply tests; s11-16 deterministic for UniAdmin tests.
                    var userId = i switch
                    {
                        < 5  => SeedIds.StudentIds[i],
                        5    => SeedIds.Student6UserId,
                        10   => SeedIds.Student11UserId,
                        11   => SeedIds.Student12UserId,
                        12   => SeedIds.Student13UserId,
                        13   => SeedIds.Student14UserId,
                        14   => SeedIds.Student15UserId,
                        15   => SeedIds.Student16UserId,
                        _    => Guid.NewGuid()
                    };
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                    var user = new User(userId, userCode, studentEmails[i], studentNames[i], UserRole.Student, passHash);
                    var gender = i % 2 == 0 ? UserGender.Male : UserGender.Female;
                    user.UpdateProfile(user.FullName, $"098765{phoneCounter++}", null, gender, new DateOnly(2004, 1, 1), "Hà Nội");
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(studentEmails[i]);

                    var uni = universityList.First(u => u.Code == "FPTU");
                    _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user.UserId, UniversityId = uni.UniversityId });
                    _context.Students.Add(new Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = user.UserId,
                        InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS,
                        Major = studentMajors[i],
                        ClassName = studentClasses[i]
                    });
                }
            }

            // Add the dev test student
            var devEmail = "trunguyen.104@gmail.com";
            if (!existingEmails.Contains(devEmail))
            {
                var userId = Guid.NewGuid();
                var userCode = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                var user = new User(userId, userCode, devEmail, "Nguyễn Trung Nguyên", UserRole.Student, passHash);
                user.UpdateProfile(user.FullName, $"098765{phoneCounter++}", null, UserGender.Male, new DateOnly(2002, 1, 1), "Hà Nội");
                user.SetStatus(UserStatus.Active);
                _context.Users.Add(user);
                _context.Students.Add(new Student { StudentId = Guid.NewGuid(), UserId = user.UserId, InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS, Major = "Software Engineering", ClassName = "SE1616" });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedTerms()
        {
            var fptu = await _context.Universities.FirstAsync(u => u.Code == "FPTU");
            var fptuCt = await _context.Universities.FirstAsync(u => u.Code == "FPTU-CT");

            var fall2025 = await _context.Terms.FirstOrDefaultAsync(t => t.UniversityId == fptu.UniversityId && t.Name == "Fall 2025");
            if (fall2025 == null)
            {
                fall2025 = new Term
                {
                    TermId = Guid.NewGuid(),
                    UniversityId = fptu.UniversityId,
                    Name = "Fall 2025",
                    StartDate = new DateOnly(2025, 9, 1),
                    EndDate = new DateOnly(2025, 12, 31),
                    Status = TermStatus.Closed
                };
                _context.Terms.Add(fall2025);
            }

            var spring2026 = await _context.Terms.FirstOrDefaultAsync(t => t.UniversityId == fptu.UniversityId && t.Name == "Spring 2026");
            if (spring2026 == null)
            {
                spring2026 = new Term
                {
                    TermId = Guid.NewGuid(),
                    UniversityId = fptu.UniversityId,
                    Name = "Spring 2026",
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 4, 30),
                    Status = TermStatus.Open
                };
                _context.Terms.Add(spring2026);
            }

            var summer2026 = await _context.Terms.FirstOrDefaultAsync(t => t.UniversityId == fptu.UniversityId && t.Name == "Summer 2026");
            if (summer2026 == null)
            {
                summer2026 = new Term
                {
                    TermId = Guid.NewGuid(),
                    UniversityId = fptu.UniversityId,
                    Name = "Summer 2026",
                    StartDate = new DateOnly(2026, 5, 1),
                    EndDate = new DateOnly(2026, 8, 31),
                    Status = TermStatus.Open
                };
                _context.Terms.Add(summer2026);
            }

            var spring2026Ct = await _context.Terms.FirstOrDefaultAsync(t => t.UniversityId == fptuCt.UniversityId && t.Name == "Spring 2026");
            if (spring2026Ct == null)
            {
                spring2026Ct = new Term
                {
                    TermId = Guid.NewGuid(),
                    UniversityId = fptuCt.UniversityId,
                    Name = "Spring 2026",
                    StartDate = new DateOnly(2026, 2, 1),
                    EndDate = new DateOnly(2026, 5, 31),
                    Status = TermStatus.Open
                };
                _context.Terms.Add(spring2026Ct);
            }

            await _context.SaveChangesAsync();

            // Enroll tất cả 10 sinh viên vào Spring 2026 (FPTU) với PlacementStatus.Placed
            // → đã được accept vào doanh nghiệp nhưng chưa có nhóm
            var allStudents = await _context.Students
                .Include(s => s.User)
                .Where(s => s.User.Email.StartsWith("student") && s.User.Email.EndsWith("@fptu.edu.vn"))
                .ToListAsync();

            foreach (var student in allStudents)
            {
                if (!await _context.StudentTerms.AnyAsync(st => st.StudentId == student.StudentId && st.TermId == spring2026.TermId))
                {
                    _context.StudentTerms.Add(new StudentTerm
                    {
                        StudentTermId = Guid.NewGuid(),
                        StudentId = student.StudentId,
                        TermId = spring2026.TermId,
                        EnrollmentStatus = EnrollmentStatus.Active,
                        PlacementStatus = PlacementStatus.Placed,
                        EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60))
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedInternshipPhases()
        {
            var fsoft = await _context.Enterprises.FirstAsync(e => e.Name == "FPT Software");
            var rikkeisoft = await _context.Enterprises.FirstAsync(e => e.Name == "Rikkeisoft");

            // Helper: create a phase via new API (majorFields + capacity), then advance status
            async Task EnsurePhase(
                Guid enterpriseId, string name,
                DateOnly start, DateOnly end,
                string majorFields, int capacity, string? description,
                InternshipPhaseStatus targetStatus)
            {
                if (await _context.InternshipPhases
                        .AnyAsync(p => p.EnterpriseId == enterpriseId && p.Name == name))
                    return;

                // Create always starts at Draft
                var phase = InternshipPhase.Create(enterpriseId, name, start, end, majorFields, capacity, description);

                // Advance to target status via UpdateInfo
                if (targetStatus != InternshipPhaseStatus.Draft)
                    phase.UpdateInfo(name, start, end, majorFields, capacity, description, targetStatus);

                _context.InternshipPhases.Add(phase);
            }

            // ── FPT Software phases ──────────────────────────────────────────────
            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Fall 2025",
                new DateOnly(2025, 9, 1), new DateOnly(2025, 12, 31),
                "Software Engineering,Information Technology", 30,
                "Đợt thực tập Fall 2025 của FPT Software — đã kết thúc",
                InternshipPhaseStatus.Closed);

            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Spring 2026",
                new DateOnly(2026, 1, 15), new DateOnly(2026, 4, 30),
                "Software Engineering,Information Technology,Computer Science", 50,
                "Đợt thực tập Spring 2026 của FPT Software — đang diễn ra",
                InternshipPhaseStatus.InProgress);

            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Summer 2026",
                new DateOnly(2026, 5, 1), new DateOnly(2026, 8, 31),
                "Software Engineering,Information Technology", 40,
                "Đợt thực tập Summer 2026 của FPT Software — đang tuyển",
                InternshipPhaseStatus.Open);

            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Winter 2026",
                new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31),
                "Software Engineering", 20,
                "Đợt thực tập Winter 2026 của FPT Software — bản nháp (edge case: Draft phase)",
                InternshipPhaseStatus.Draft);

            // ── Rikkeisoft phases ────────────────────────────────────────────────
            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Fall 2025",
                new DateOnly(2025, 9, 1), new DateOnly(2025, 12, 31),
                "Software Engineering,Information Technology", 20,
                "Đợt thực tập Fall 2025 của Rikkeisoft — đã kết thúc",
                InternshipPhaseStatus.Closed);

            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Spring 2026",
                new DateOnly(2026, 2, 1), new DateOnly(2026, 5, 31),
                "Software Engineering,Computer Science", 20,
                "Đợt thực tập Spring 2026 của Rikkeisoft — đang diễn ra",
                InternshipPhaseStatus.InProgress);

            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Summer 2026",
                new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 30),
                "Information Technology", 15,
                "Đợt thực tập Summer 2026 của Rikkeisoft — bản nháp",
                InternshipPhaseStatus.Draft);

            await _context.SaveChangesAsync();
        }

        private async Task SeedInternshipGroups()
        {
            var fsoft = await _context.Enterprises.FirstAsync(e => e.Name == "FPT Software");
            var rikkeisoft = await _context.Enterprises.FirstAsync(e => e.Name == "Rikkeisoft");
            // Dùng EnterpriseUserId cố định từ SeedIds thay vì query DB — tránh race condition khi seed chưa commit
            var mentorFptEuId    = SeedIds.MentorFptEuId;
            var mentorRikkeisEuId = SeedIds.MentorRikkeisoftEuId;

            // Resolve phases by name (idempotent, no hardcoded Guid constraint)
            var phaseInProgressFpt = await _context.InternshipPhases.FirstAsync(
                p => p.EnterpriseId == fsoft.EnterpriseId && p.Name == "FPT Software Spring 2026");
            var phaseClosedFpt = await _context.InternshipPhases.FirstAsync(
                p => p.EnterpriseId == fsoft.EnterpriseId && p.Name == "FPT Software Fall 2025");
            var phaseOpenFpt = await _context.InternshipPhases.FirstAsync(
                p => p.EnterpriseId == fsoft.EnterpriseId && p.Name == "FPT Software Summer 2026");
            var phaseInProgressRikkei = await _context.InternshipPhases.FirstAsync(
                p => p.EnterpriseId == rikkeisoft.EnterpriseId && p.Name == "Rikkeisoft Spring 2026");
            // BUG-10 FIX: Use Rikkeisoft's own closed phase, not FPT's
            var phaseClosedRikkei = await _context.InternshipPhases.FirstAsync(
                p => p.EnterpriseId == rikkeisoft.EnterpriseId && p.Name == "Rikkeisoft Fall 2025");

            var s1 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student1@fptu.edu.vn");
            var s2 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s4 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");

            var spring2026 = await _context.Terms.FirstAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");
            var fall2025 = await _context.Terms.FirstAsync(t => t.Name == "Fall 2025" && t.University.Code == "FPTU");
            var spring2026Ct = await _context.Terms.Include(t => t.University).FirstOrDefaultAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU-CT");

            // FPT Software OJT Team -> FPT Software OJT Team Alpha
            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            if (group3 == null)
            {
                group3 = InternshipGroup.Create(phaseInProgressFpt.PhaseId, "FPT Software OJT Team Alpha", "Next-gen platform development", fsoft.EnterpriseId, mentorFptEuId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow.AddMonths(3));
                group3.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(group3);
            }

            var group5 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
            if (group5 == null)
            {
                group5 = InternshipGroup.Create(phaseClosedRikkei.PhaseId, "Rikkeisoft CRM Legacy", "Maintenance of legacy CRM", rikkeisoft.EnterpriseId, mentorRikkeisEuId, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(-2));
                group5.UpdateStatus(GroupStatus.Finished);
                _context.InternshipGroups.Add(group5);
            }

            var rikkeiActiveGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");
            if (rikkeiActiveGroup == null)
            {
                rikkeiActiveGroup = InternshipGroup.Create(phaseInProgressRikkei.PhaseId, "Rikkeisoft Spring 2026 Team", "Backend modernization and internal platform work", rikkeisoft.EnterpriseId, mentorRikkeisEuId, DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddMonths(2));
                rikkeiActiveGroup.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(rikkeiActiveGroup);
            }

            InternshipGroup? fptCtGroup = null;
            if (spring2026Ct != null)
            {
                fptCtGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software CT OJT Team");
                if (fptCtGroup == null)
                {
                    fptCtGroup = InternshipGroup.Create(phaseInProgressFpt.PhaseId, "FPT Software CT OJT Team", "Cross-campus internship squad for FPTU Can Tho", fsoft.EnterpriseId, mentorFptEuId, DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddMonths(2));
                    fptCtGroup.UpdateStatus(GroupStatus.Active);
                    _context.InternshipGroups.Add(fptCtGroup);
                }
            }
            
            var archivedGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Archived Project");
            if (archivedGroup == null)
            {
                archivedGroup = InternshipGroup.Create(phaseClosedFpt.PhaseId, "FPT Archived Project", "Old project idea", fsoft.EnterpriseId, mentorFptEuId, DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow.AddMonths(-10));
                archivedGroup.UpdateStatus(GroupStatus.Archived);
                _context.InternshipGroups.Add(archivedGroup);
            }

            // FPT OJT Team Beta — owned by AltMentor, test "mentor chỉ thấy project của mình"
            var fptAltBetaGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Beta");
            if (fptAltBetaGroup == null)
            {
                fptAltBetaGroup = InternshipGroup.Create(phaseInProgressFpt.PhaseId, "FPT Software OJT Team Beta", "Data engineering track for FPT Spring 2026", fsoft.EnterpriseId, SeedIds.MentorFptAltEuId, DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddMonths(3));
                fptAltBetaGroup.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(fptAltBetaGroup);
            }

            await _context.SaveChangesAsync();

            // DO NOT ADD TO InternshipStudents (As per commit instructions)

            // Seed some applications
            // Ensure JobId is set for each seeded application to satisfy FK constraint (job_id is required)
            var fptJob = await _context.Jobs.FirstAsync(j => j.EnterpriseId == SeedIds.FptSoftwareId);
            var rikkeiJob = await _context.Jobs.FirstAsync(j => j.EnterpriseId == SeedIds.RikkeisoftId);

            _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s3.StudentId, JobId = fptJob.JobId, Status = InternshipApplicationStatus.Placed, AppliedAt = DateTime.UtcNow.AddDays(-40) });
            _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s2.StudentId, JobId = rikkeiJob.JobId, Status = InternshipApplicationStatus.PendingAssignment, AppliedAt = DateTime.UtcNow.AddDays(-10) });
            // [NEW] Seed Pending and Rejected Applications
            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == rikkeisoft.EnterpriseId && a.StudentId == s4.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s4.StudentId, JobId = rikkeiJob.JobId, Status = InternshipApplicationStatus.Applied, AppliedAt = DateTime.UtcNow.AddDays(-2) });
            }

            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.StudentId == s2.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = fall2025.TermId, StudentId = s2.StudentId, JobId = fptJob.JobId, Status = InternshipApplicationStatus.Rejected, RejectReason = "Not a good fit for this semester", AppliedAt = DateTime.UtcNow.AddDays(-100) });
            }

            await _context.SaveChangesAsync();

            // ── Thêm InternshipApplication Approved cho từng sinh viên ─────────────
            // API GetPlacedStudents query từ InternshipApplication (Status=Approved),
            // vì vậy mỗi sinh viên PHẢI có 1 application Approved để xuất hiện trong danh sách.
            //
            // Phân công: 6 SV → FPT Software, 4 SV → Rikkeisoft
            var allStudents = await _context.Students
                .Include(s => s.User)
                .Where(s => s.User.Email.StartsWith("student") && s.User.Email.EndsWith("@fptu.edu.vn"))
                .OrderBy(s => s.User.Email)   // student1, student10, student2, ... student9
                .ToListAsync();

            // Sắp xếp theo số thứ tự: student1..10
            var orderedStudents = allStudents
                .OrderBy(s => int.Parse(System.Text.RegularExpressions.Regex
                    .Match(s.User.Email, @"\d+").Value))
                .ToList();

            // FPT Software: student1, student2, student3, student6, student8
            var fstudents = new[] { 0, 1, 2, 5, 7 };
            // Rikkeisoft:   student4, student5, student7, student9, student10
            var rstudents = new[] { 3, 4, 6, 8, 9 };

            foreach (var idx in fstudents)
            {
                if (idx >= orderedStudents.Count) continue;
                var stu = orderedStudents[idx];
                if (!await _context.InternshipApplications.AnyAsync(
                    a => a.EnterpriseId == fsoft.EnterpriseId
                      && a.TermId == spring2026.TermId
                      && a.StudentId == stu.StudentId))
                {
                    _context.InternshipApplications.Add(new InternshipApplication
                    {
                        ApplicationId = Guid.NewGuid(),
                        EnterpriseId = fsoft.EnterpriseId,
                        TermId = spring2026.TermId,
                        StudentId = stu.StudentId,
                        JobId = fptJob.JobId,
                        Status = InternshipApplicationStatus.Placed,
                        AppliedAt = DateTime.UtcNow.AddDays(-30)
                    });
                }
            }

            foreach (var idx in rstudents)
            {
                if (idx >= orderedStudents.Count) continue;
                var stu = orderedStudents[idx];
                if (!await _context.InternshipApplications.AnyAsync(
                    a => a.EnterpriseId == rikkeisoft.EnterpriseId
                      && a.TermId == spring2026.TermId
                      && a.StudentId == stu.StudentId))
                {
                    _context.InternshipApplications.Add(new InternshipApplication
                    {
                        ApplicationId = Guid.NewGuid(),
                        EnterpriseId = rikkeisoft.EnterpriseId,
                        TermId = spring2026.TermId,
                        StudentId = stu.StudentId,
                        JobId = rikkeiJob.JobId,
                        Status = InternshipApplicationStatus.Placed,
                        AppliedAt = DateTime.UtcNow.AddDays(-25)
                    });
                }
            }

            await _context.SaveChangesAsync();
            // Không thêm sinh viên vào bất kỳ nhóm nào.
            // Tất cả 10 SV đã có Approved application → xuất hiện trong GetPlacedStudents.
            // HR có thể thêm từng SV vào nhóm qua API AddStudentsToGroup.
        }

        private async Task SeedProjectsAndWorkItems()
        {
            if (await _context.Projects.AnyAsync()) return;

            var group3 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            var group5 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
            var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");

            // Project 3
            var proj3 = Project.Create("IOC v2.0 Platform", "Centralized internship management system", "PRJ-FPTSOF_FPT_1", "CNTT", "Develop a centralized internship management platform.", mentorId: SeedIds.MentorFptEuId);
            proj3.AssignToGroup(group3.InternshipId, DateTime.UtcNow.AddMonths(-1).AddDays(5), null);
            proj3.Publish();
            _context.Projects.Add(proj3);

            // Project 5
            var proj5 = Project.Create("Legacy CRM Maintenance", "Fixing bugs and optimizing older modules", "PRJ-RIKKE_RIKK_1", "CNTT", "Fix bugs and optimize legacy modules.", mentorId: SeedIds.MentorRikkeisoftEuId);
            proj5.AssignToGroup(group5.InternshipId, DateTime.UtcNow.AddMonths(-6).AddDays(5), DateTime.UtcNow.AddMonths(-2).AddDays(-5));
            proj5.Publish();
            proj5.SetOperationalStatus(OperationalStatus.Completed);
            _context.Projects.Add(proj5);

            await _context.SaveChangesAsync();

            // Sprints
            var sprint3 = new Sprint(proj3.ProjectId, "Sprint 1: Auth & Base", "Setup auth foundations");
            sprint3.Start(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)));
            _context.Sprints.Add(sprint3);

            var sprint5 = new Sprint(proj5.ProjectId, "Final Polish", "Release documentation");
            sprint5.Start(DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3)), DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2).AddDays(-10)));
            sprint5.Complete();
            _context.Sprints.Add(sprint5);

            // Work Items for S3
            _context.WorkItems.AddRange(
                new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3.ProjectId, Title = "Design DB Schema", Type = WorkItemType.Task, Status = WorkItemStatus.Done, AssigneeId = s3.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)) },
                new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3.ProjectId, Title = "Implement JWT", Type = WorkItemType.Task, Status = WorkItemStatus.InProgress, AssigneeId = s3.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)) },
                new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3.ProjectId, Title = "Unit Testing Auth", Type = WorkItemType.Task, Status = WorkItemStatus.Todo, AssigneeId = s3.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)) } // Fixed: ToDo -> Todo
            );

            // Work Items for S5
            for (int i = 1; i <= 5; i++)
            {
                _context.WorkItems.Add(new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj5.ProjectId, Title = $"Legacy fix #{i}", Type = WorkItemType.Task, Status = WorkItemStatus.Done, AssigneeId = s5.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3).AddDays(i * 10)) });
            }

            // [NEW] Seed Pending Project, Cancelled Sprint and WorkItems with different statuses
            var projPending = Project.Create("FPT Future System", "Next phase architecture", "PRJ-FPTSOF_FPT_2", "CNTT", "Design next phase architecture.", mentorId: SeedIds.MentorFptEuId);
            projPending.AssignToGroup(group3.InternshipId, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(30));
            // Draft by default — no Publish() call
            if (!await _context.Projects.AnyAsync(p => p.ProjectName == "FPT Future System"))
            {
                _context.Projects.Add(projPending);
                
                var cancelledSprint = new Sprint(projPending.ProjectId, "Cancelled Sprint", "A sprint that was planned but cancelled");
                cancelledSprint.Start(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));
                _context.Sprints.Add(cancelledSprint);

                _context.WorkItems.AddRange(
                    new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = projPending.ProjectId, Title = "Initial Design", Type = WorkItemType.Task, Status = WorkItemStatus.Todo, AssigneeId = null, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)) }
                );

                var requirementsTask = new WorkItem
                {
                    WorkItemId = Guid.NewGuid(),
                    ProjectId = projPending.ProjectId,
                    Title = "Gather Requirements",
                    Type = WorkItemType.Task,
                    Status = WorkItemStatus.Cancelled,
                    AssigneeId = null,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15))
                };
                _context.WorkItems.Add(requirementsTask);

                _context.WorkItems.Add(new WorkItem
                {
                    WorkItemId = Guid.NewGuid(),
                    ProjectId = projPending.ProjectId,
                    ParentId = requirementsTask.WorkItemId,
                    Title = "Interview Stakeholders",
                    Type = WorkItemType.Task,
                    Status = WorkItemStatus.Todo,
                    Priority = Priority.High,
                    StoryPoint = 3
                });
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed dữ liệu cho feature Manage-IG-Project:
        /// - Projects cho các nhóm Active chưa có dự án (Rikkeisoft Spring, FPT CT)
        /// - Project archived gắn với nhóm Archived
        /// - Orphan project (InternshipId = null) — nhóm bị xóa/archived
        /// </summary>
        private async Task SeedManageIGProjectData()
        {
            // Rikkeisoft Spring 2026 Team — Active group, chưa có project
            var rikkeiGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");
            if (rikkeiGroup != null && !await _context.Projects.AnyAsync(p => p.InternshipId == rikkeiGroup.InternshipId))
            {
                // Project 1: Published + Active
                var rikkeiProj1 = Project.Create(
                    "Rikkeisoft Internal Portal",
                    "Xây dựng cổng thông tin nội bộ cho nhân viên Rikkeisoft",
                    "PRJ-RIKKES_RIKK_2",
                    "Công nghệ thông tin",
                    "Phát triển portal nội bộ: quản lý nhân sự, leave request, timesheet.",
                    mentorId: SeedIds.MentorRikkeisoftEuId);
                rikkeiProj1.AssignToGroup(rikkeiGroup.InternshipId, DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddMonths(2));
                rikkeiProj1.Publish();
                _context.Projects.Add(rikkeiProj1);

                // Project 2: Draft + Active — chưa publish
                var rikkeiProj2 = Project.Create(
                    "Rikkeisoft Mobile App",
                    "Ứng dụng di động cho khách hàng của Rikkeisoft",
                    "PRJ-RIKKES_RIKK_3",
                    "Mobile",
                    "Phát triển ứng dụng mobile cross-platform bằng Flutter.",
                    mentorId: SeedIds.MentorRikkeisoftEuId);
                rikkeiProj2.AssignToGroup(rikkeiGroup.InternshipId, null, null);
                _context.Projects.Add(rikkeiProj2);
            }

            // FPT Software CT OJT Team — Active group, chưa có project
            var fptCtGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software CT OJT Team");
            if (fptCtGroup != null && !await _context.Projects.AnyAsync(p => p.InternshipId == fptCtGroup.InternshipId))
            {
                var fptCtProj = Project.Create(
                    "FPT CT Smart Campus",
                    "Hệ thống quản lý khuôn viên thông minh cho FPTU Cần Thơ",
                    "PRJ-FPTSOF_FPT_3",
                    "IoT / CNTT",
                    "Tích hợp IoT, camera AI và hệ thống điểm danh tự động.",
                    mentorId: SeedIds.MentorFptEuId);
                fptCtProj.AssignToGroup(fptCtGroup.InternshipId, null, null);
                _context.Projects.Add(fptCtProj);
            }

            // FPT Archived Project group — project cũng nên Archived
            var archivedGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Archived Project");
            if (archivedGroup != null && !await _context.Projects.AnyAsync(p => p.InternshipId == archivedGroup.InternshipId))
            {
                var archivedProj = Project.Create(
                    "FPT Legacy HR System",
                    "Hệ thống HR cũ đã ngừng phát triển",
                    "PRJ-FPTSOF_FPT_4",
                    "Hệ thống doanh nghiệp",
                    "Duy trì và hỗ trợ hệ thống HR cũ.",
                    mentorId: SeedIds.MentorFptEuId);
                archivedProj.AssignToGroup(archivedGroup.InternshipId, DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow.AddMonths(-10));
                archivedProj.Publish();
                archivedProj.SetOperationalStatus(OperationalStatus.Archived);
                _context.Projects.Add(archivedProj);
            }

            // Orphan project — bị orphan sau khi nhóm bị xóa (IsOrphaned = true, InternshipId = null)
            if (!await _context.Projects.AnyAsync(p => p.ProjectName == "Orphan Research Project"))
            {
                var orphanProj = Project.Create(
                    "Orphan Research Project",
                    "Dự án nghiên cứu bị orphan do nhóm thực tập đã bị xóa",
                    "PRJ-ORPHAN_001",
                    "Nghiên cứu",
                    "Nghiên cứu ứng dụng AI trong kiểm thử phần mềm.",
                    mentorId: SeedIds.MentorFptEuId);
                orphanProj.SetOrphan(); // simulate: group bị xóa sau khi assign
                orphanProj.Publish();  // Published nhưng orphan — test badge AC-16
                _context.Projects.Add(orphanProj);
            }

            // Project Draft/Unstarted chưa gán nhóm — test case "Chưa gán nhóm"
            if (!await _context.Projects.AnyAsync(p => p.ProjectName == "FPT AI Code Review Tool"))
            {
                var draftUnassigned = Project.Create(
                    "FPT AI Code Review Tool",
                    "Công cụ review code tự động sử dụng AI cho intern FPT Software",
                    "PRJ-FPTSOF_FPT_5",
                    "CNTT",
                    "Xây dựng tool phân tích code, phát hiện bug và gợi ý cải thiện bằng LLM.",
                    mentorId: SeedIds.MentorFptEuId);
                // Draft + Unstarted + không gắn nhóm — đại diện cho project mới tạo, chưa assign group
                _context.Projects.Add(draftUnassigned);
            }

            // Project owned by AltMentor (MentorFptAltEuId) — test "mentor chỉ thấy project của mình"
            var fptAltGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Beta");
            if (fptAltGroup != null && !await _context.Projects.AnyAsync(p => p.ProjectName == "FPT Analytics Dashboard"))
            {
                var altMentorProj = Project.Create(
                    "FPT Analytics Dashboard",
                    "Dashboard phân tích dữ liệu intern cho FPT Software Team Beta",
                    "PRJ-FPTSOF_FPT_6",
                    "Data Engineering",
                    "Xây dựng dashboard visualize KPI và tiến độ dự án bằng React + ECharts.",
                    mentorId: SeedIds.MentorFptAltEuId);
                altMentorProj.AssignToGroup(fptAltGroup.InternshipId, DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddMonths(3));
                altMentorProj.Publish();
                _context.Projects.Add(altMentorProj);
                await _context.SaveChangesAsync(); // flush để có ProjectId

                var altProj = await _context.Projects.FirstAsync(p => p.ProjectName == "FPT Analytics Dashboard");
                if (!await _context.Sprints.AnyAsync(s => s.ProjectId == altProj.ProjectId))
                {
                    // Sprint variety: Completed + Active + Planned
                    var sprint1 = new Sprint(altProj.ProjectId, "Sprint 1 – Data Collection", "Thu thập và làm sạch dữ liệu nguồn");
                    sprint1.Start(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-8)));
                    sprint1.Complete();

                    var sprint2 = new Sprint(altProj.ProjectId, "Sprint 2 – Visualization", "Xây dựng các chart components");
                    sprint2.Start(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));

                    var sprint3 = new Sprint(altProj.ProjectId, "Sprint 3 – Integration", "Tích hợp API và deploy staging");
                    // Planned = default

                    _context.Sprints.AddRange(sprint1, sprint2, sprint3);
                    await _context.SaveChangesAsync();

                    // WorkItem type variety (Epic, UserStory, Task, Subtask) for the project
                    if (!await _context.WorkItems.AnyAsync(w => w.ProjectId == altProj.ProjectId))
                    {
                        _context.WorkItems.AddRange(
                            new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = altProj.ProjectId, Type = WorkItemType.Epic,      Title = "Dashboard Module",                  Priority = Priority.High,   BacklogOrder = 1 },
                            new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = altProj.ProjectId, Type = WorkItemType.UserStory,  Title = "Hiển thị KPI tổng quan",           Priority = Priority.High,   BacklogOrder = 2 },
                            new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = altProj.ProjectId, Type = WorkItemType.Task,       Title = "Tích hợp ECharts bar chart",        Priority = Priority.Medium, BacklogOrder = 3 },
                            new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = altProj.ProjectId, Type = WorkItemType.Task,       Title = "Fix lỗi data null khi filter",      Priority = Priority.High,   BacklogOrder = 4 },
                            new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = altProj.ProjectId, Type = WorkItemType.Subtask,    Title = "Nghiên cứu React Query caching",    Priority = Priority.Low,    BacklogOrder = 5 }
                        );
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed sinh viên vào các nhóm Active để test UpdateProject (assignment count),
        /// AddStudentsToGroup, MoveStudentsBetweenGroups, RemoveStudentsFromGroup.
        /// </summary>
        private async Task SeedInternshipStudents()
        {
            var fptGroup = await _context.InternshipGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            var rikkeiGroup = await _context.InternshipGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");

            if (fptGroup == null || rikkeiGroup == null) return;

            var s1 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student1@fptu.edu.vn");
            var s2 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s6 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student6@fptu.edu.vn");
            var s7 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student7@fptu.edu.vn");
            var s8 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student8@fptu.edu.vn");
            var s9 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student9@fptu.edu.vn");
            var s10 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student10@fptu.edu.vn");
            var s4 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");

            var spring2026 = await _context.Terms
                .Include(t => t.University)
                .FirstOrDefaultAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");

            if (spring2026 != null)
            {
                var assignedEnterpriseByEmail = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["student1@fptu.edu.vn"] = SeedIds.FptSoftwareId,
                    ["student2@fptu.edu.vn"] = SeedIds.FptSoftwareId,
                    ["student3@fptu.edu.vn"] = SeedIds.FptSoftwareId,
                    ["student6@fptu.edu.vn"] = SeedIds.FptSoftwareId,
                    ["student8@fptu.edu.vn"] = SeedIds.FptSoftwareId,
                    ["student4@fptu.edu.vn"] = SeedIds.RikkeisoftId,
                    ["student5@fptu.edu.vn"] = SeedIds.RikkeisoftId,
                    ["student7@fptu.edu.vn"] = SeedIds.RikkeisoftId,
                    ["student9@fptu.edu.vn"] = SeedIds.RikkeisoftId,
                    ["student10@fptu.edu.vn"] = SeedIds.RikkeisoftId
                };

                var studentTerms = await _context.StudentTerms
                    .Include(st => st.Student)
                        .ThenInclude(s => s.User)
                    .Where(st => st.TermId == spring2026.TermId
                                 && st.Student.User.Email.StartsWith("student")
                                 && st.Student.User.Email.EndsWith("@fptu.edu.vn")
                                 && st.DeletedAt == null)
                    .ToListAsync();

                foreach (var studentTerm in studentTerms)
                {
                    if (assignedEnterpriseByEmail.TryGetValue(studentTerm.Student.User.Email, out var enterpriseId)
                        && studentTerm.EnterpriseId != enterpriseId)
                    {
                        studentTerm.EnterpriseId = enterpriseId;
                    }
                }
            }

            // FPT Software OJT Team Alpha: s1(Leader), s2/s3/s6/s8(Member)
            bool fptHasStudents = await _context.InternshipStudents.AnyAsync(m => m.InternshipId == fptGroup.InternshipId);
            if (!fptHasStudents)
            {
                if (s1 != null) fptGroup.AddMember(s1.StudentId, InternshipRole.Leader);
                if (s2 != null) fptGroup.AddMember(s2.StudentId, InternshipRole.Member);
                if (s3 != null) fptGroup.AddMember(s3.StudentId, InternshipRole.Member);
                if (s6 != null) fptGroup.AddMember(s6.StudentId, InternshipRole.Member);
                if (s8 != null) fptGroup.AddMember(s8.StudentId, InternshipRole.Member);
                _context.InternshipGroups.Update(fptGroup);
            }

            // Rikkeisoft Spring 2026 Team: s4(Leader), s5/s7/s9/s10(Member)
            bool rikkeiHasStudents = await _context.InternshipStudents.AnyAsync(m => m.InternshipId == rikkeiGroup.InternshipId);
            if (!rikkeiHasStudents)
            {
                if (s4 != null) rikkeiGroup.AddMember(s4.StudentId, InternshipRole.Leader);
                if (s5 != null) rikkeiGroup.AddMember(s5.StudentId, InternshipRole.Member);
                if (s7 != null) rikkeiGroup.AddMember(s7.StudentId, InternshipRole.Member);
                if (s9 != null) rikkeiGroup.AddMember(s9.StudentId, InternshipRole.Member);
                if (s10 != null) rikkeiGroup.AddMember(s10.StudentId, InternshipRole.Member);
                _context.InternshipGroups.Update(rikkeiGroup);
            }

            // Keep deterministic joined dates for UI test scenarios:
            // - s3: enough logbooks despite many violations
            // - s7: few violations but many missing logbooks
            var s3Membership = await _context.InternshipStudents
                .FirstOrDefaultAsync(m => m.InternshipId == fptGroup.InternshipId && s3 != null && m.StudentId == s3.StudentId);
            if (s3Membership != null)
            {
                var s3TargetJoinedAt = DateTime.UtcNow.Date.AddDays(-4);
                if (s3Membership.JoinedAt > s3TargetJoinedAt)
                {
                    s3Membership.JoinedAt = s3TargetJoinedAt;
                }
            }

            var s7Membership = await _context.InternshipStudents
                .FirstOrDefaultAsync(m => m.InternshipId == rikkeiGroup.InternshipId && s7 != null && m.StudentId == s7.StudentId);
            if (s7Membership != null)
            {
                var s7TargetJoinedAt = DateTime.UtcNow.Date.AddDays(-12);
                if (s7Membership.JoinedAt > s7TargetJoinedAt)
                {
                    s7Membership.JoinedAt = s7TargetJoinedAt;
                }
            }

            var s8Membership = await _context.InternshipStudents
                .FirstOrDefaultAsync(m => m.InternshipId == fptGroup.InternshipId && s8 != null && m.StudentId == s8.StudentId);
            if (s8Membership != null)
            {
                var s8TargetJoinedAt = DateTime.UtcNow.Date.AddDays(-5);
                if (s8Membership.JoinedAt > s8TargetJoinedAt)
                {
                    s8Membership.JoinedAt = s8TargetJoinedAt;
                }
            }

            var s9Membership = await _context.InternshipStudents
                .FirstOrDefaultAsync(m => m.InternshipId == rikkeiGroup.InternshipId && s9 != null && m.StudentId == s9.StudentId);
            if (s9Membership != null)
            {
                var s9TargetJoinedAt = DateTime.UtcNow.Date.AddDays(-8);
                if (s9Membership.JoinedAt > s9TargetJoinedAt)
                {
                    s9Membership.JoinedAt = s9TargetJoinedAt;
                }
            }

            var s10Membership = await _context.InternshipStudents
                .FirstOrDefaultAsync(m => m.InternshipId == rikkeiGroup.InternshipId && s10 != null && m.StudentId == s10.StudentId);
            if (s10Membership != null)
            {
                var s10TargetJoinedAt = DateTime.UtcNow.Date.AddDays(-9);
                if (s10Membership.JoinedAt > s10TargetJoinedAt)
                {
                    s10Membership.JoinedAt = s10TargetJoinedAt;
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Seeds UniAdmin monitoring test scenarios for students s11–s16.
        /// Must run AFTER SeedInternshipStudents (so StudentTerms exist).
        ///
        /// Scenarios:
        ///   s11 – NoGroup        : Placed + EnterpriseId set, but no group membership
        ///   s12 – PendingConf.   : Unplaced + pending application
        ///   s13 – Unplaced       : Unplaced, no pending application
        ///   s14 – Completed      : in Finished group (UniAdmin Completed Group)
        ///   s15 – 100% Logbook   : Active group, all logbooks submitted
        ///   s16 – NoMentor       : Active group with MentorId = null
        /// </summary>
        private async Task SeedUniAdminTestScenarios()
        {
            // Guard: skip if already seeded (check by group name)
            if (await _context.InternshipGroups.AnyAsync(g => g.GroupName == "UniAdmin Completed Group"))
                return;

            var fsoft = await _context.Enterprises.FirstAsync(e => e.Name == "FPT Software");
            var spring2026 = await _context.Terms
                .Include(t => t.University)
                .FirstAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");

            var phaseInProgress = await _context.InternshipPhases.FirstAsync(
                p => p.EnterpriseId == fsoft.EnterpriseId && p.Name == "FPT Software Spring 2026");
            var phaseClosed = await _context.InternshipPhases.FirstAsync(
                p => p.EnterpriseId == fsoft.EnterpriseId && p.Name == "FPT Software Fall 2025");

            var fptJob = await _context.Jobs.FirstAsync(j => j.EnterpriseId == fsoft.EnterpriseId);

            // Load students
            var s11 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student11@fptu.edu.vn");
            var s12 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student12@fptu.edu.vn");
            var s13 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student13@fptu.edu.vn");
            var s14 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student14@fptu.edu.vn");
            var s15 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student15@fptu.edu.vn");
            var s16 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student16@fptu.edu.vn");

            // ──────────────────────────────────────────────────────────────────
            // Fix StudentTerms: SeedTerms enrolls ALL students with PlacementStatus.Placed.
            // s12 and s13 need Unplaced; s11/s14/s15/s16 need EnterpriseId set for group matching.
            // ──────────────────────────────────────────────────────────────────
            var studentTerms = await _context.StudentTerms
                .Where(st => st.TermId == spring2026.TermId
                          && (st.StudentId == s11.StudentId
                           || st.StudentId == s12.StudentId
                           || st.StudentId == s13.StudentId
                           || st.StudentId == s14.StudentId
                           || st.StudentId == s15.StudentId
                           || st.StudentId == s16.StudentId)
                          && st.DeletedAt == null)
                .ToListAsync();

            foreach (var st in studentTerms)
            {
                if (st.StudentId == s12.StudentId || st.StudentId == s13.StudentId)
                {
                    st.PlacementStatus = PlacementStatus.Unplaced;
                    st.EnterpriseId = null;
                }
                else
                {
                    // s11/s14/s15/s16: Placed at FPT Software
                    st.EnterpriseId = fsoft.EnterpriseId;
                }
            }

            await _context.SaveChangesAsync();

            // ──────────────────────────────────────────────────────────────────
            // s12: PendingConfirmation — Unplaced + pending application
            // ──────────────────────────────────────────────────────────────────
            if (!await _context.InternshipApplications.AnyAsync(
                a => a.StudentId == s12.StudentId && a.TermId == spring2026.TermId
                  && (a.Status == InternshipApplicationStatus.Applied || a.Status == InternshipApplicationStatus.PendingAssignment)))
            {
                _context.InternshipApplications.Add(new InternshipApplication
                {
                    ApplicationId = Guid.NewGuid(),
                    EnterpriseId = fsoft.EnterpriseId,
                    TermId = spring2026.TermId,
                    StudentId = s12.StudentId,
                    JobId = fptJob.JobId,
                    Status = InternshipApplicationStatus.Applied,
                    Source = ApplicationSource.SelfApply,
                    AppliedAt = DateTime.UtcNow.AddDays(-3)
                });
            }

            // ──────────────────────────────────────────────────────────────────
            // s14: Completed — Finished group (phase = Fall 2025, ended 1 month ago)
            // ──────────────────────────────────────────────────────────────────
            var completedGroup = InternshipGroup.Create(
                phaseClosed.PhaseId, "UniAdmin Completed Group",
                "Test group for Completed scenario — ended 1 month ago",
                fsoft.EnterpriseId, SeedIds.MentorFptEuId,
                DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow.AddDays(-30));
            completedGroup.UpdateStatus(GroupStatus.Finished);
            completedGroup.AddMember(s14.StudentId, InternshipRole.Member);
            _context.InternshipGroups.Add(completedGroup);

            // ──────────────────────────────────────────────────────────────────
            // s15: Active + 100% Logbook — joined 5 business days ago, all submitted
            // ──────────────────────────────────────────────────────────────────
            var logbookGroup = InternshipGroup.Create(
                phaseInProgress.PhaseId, "UniAdmin Active Group",
                "Test group for 100% logbook scenario",
                fsoft.EnterpriseId, SeedIds.MentorFptEuId,
                DateTime.UtcNow.AddDays(-14), DateTime.UtcNow.AddMonths(3));
            logbookGroup.UpdateStatus(GroupStatus.Active);
            logbookGroup.AddMember(s15.StudentId, InternshipRole.Member);
            _context.InternshipGroups.Add(logbookGroup);

            // ──────────────────────────────────────────────────────────────────
            // s16: Active + NoMentor group
            // ──────────────────────────────────────────────────────────────────
            var noMentorGroup = InternshipGroup.Create(
                phaseInProgress.PhaseId, "UniAdmin NoMentor Group",
                "Test group for NoMentor scenario — no mentor assigned",
                fsoft.EnterpriseId, mentorId: null,
                DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddMonths(3));
            noMentorGroup.UpdateStatus(GroupStatus.Active);
            noMentorGroup.AddMember(s16.StudentId, InternshipRole.Member);
            _context.InternshipGroups.Add(noMentorGroup);

            await _context.SaveChangesAsync();

            // ──────────────────────────────────────────────────────────────────
            // s15: Fix JoinedAt to 5 business days ago, then seed 100% logbooks
            // ──────────────────────────────────────────────────────────────────
            var s15Membership = await _context.InternshipStudents
                .FirstOrDefaultAsync(m => m.InternshipId == logbookGroup.InternshipId && m.StudentId == s15.StudentId);
            if (s15Membership != null)
            {
                // Set JoinedAt to 7 calendar days ago (≈5 business days)
                s15Membership.JoinedAt = DateTime.UtcNow.AddDays(-7);
                await _context.SaveChangesAsync();

                // Seed logbooks for every business day from JoinedAt to yesterday
                var joinedDate = s15Membership.JoinedAt.Date;
                var yesterday = DateTime.UtcNow.Date.AddDays(-1);
                for (var day = joinedDate; day <= yesterday; day = day.AddDays(1))
                {
                    if (day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday)
                        continue;
                    if (await _context.Logbooks.AnyAsync(l =>
                            l.InternshipId == logbookGroup.InternshipId
                            && l.StudentId == s15.StudentId
                            && l.DateReport.Date == day))
                        continue;

                    _context.Logbooks.Add(Logbook.Create(
                        logbookGroup.InternshipId,
                        s15.StudentId,
                        $"Hoàn thành các task trong sprint — {day:dd/MM/yyyy}",
                        null,
                        "Tiếp tục công việc ngày hôm sau",
                        day));
                }

                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedLogbooks()
        {
            var proj3 = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "IOC v2.0 Platform");
            var proj5 = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "Legacy CRM Maintenance");
            var rikkeiProj = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "Rikkeisoft Internal Portal");
            var s1 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student1@fptu.edu.vn");
            var s2 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s4 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");
            var s6 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student6@fptu.edu.vn");
            var s7 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student7@fptu.edu.vn");
            var s8 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student8@fptu.edu.vn");
            var s9 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student9@fptu.edu.vn");
            var s10 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student10@fptu.edu.vn");

            if (proj3 == null || proj5 == null || s3 == null || s5 == null || !proj3.InternshipId.HasValue || !proj5.InternshipId.HasValue) return;

            var proj3InternshipId = proj3.InternshipId.Value;
            var proj5InternshipId = proj5.InternshipId.Value;

            Guid? rikkeiInternshipId = rikkeiProj?.InternshipId;

            var internshipWindows = await _context.InternshipGroups
                .Where(g => g.InternshipId == proj3InternshipId
                         || g.InternshipId == proj5InternshipId
                         || (rikkeiInternshipId.HasValue && g.InternshipId == rikkeiInternshipId.Value))
                .ToDictionaryAsync(
                    g => g.InternshipId,
                    g => new { StartDate = g.StartDate?.Date, EndDate = g.EndDate?.Date });

            static DateTime GetStartOfWeek(DateTime date)
            {
                var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
                return date.Date.AddDays(-diff);
            }

            DateTime NormalizeReportDate(Guid internshipId, DateTime reportDate)
            {
                var normalized = reportDate.Date;
                var today = DateTime.UtcNow.Date;

                if (normalized > today)
                {
                    normalized = today;
                }

                if (internshipWindows.TryGetValue(internshipId, out var window))
                {
                    if (window.StartDate.HasValue && normalized < window.StartDate.Value)
                    {
                        normalized = window.StartDate.Value;
                    }

                    if (window.EndDate.HasValue && normalized > window.EndDate.Value)
                    {
                        normalized = window.EndDate.Value;
                    }
                }

                return normalized;
            }

            var currentWeekStart = GetStartOfWeek(DateTime.UtcNow.Date);
            var lastCompletedWeekStart = currentWeekStart.AddDays(-7);

            var logbookSeeds = new List<(Guid InternshipId, Guid StudentId, string Summary, string? Issue, string Plan, DateTime DateReport, bool IsLate)>();

            void AddLog(
                Guid internshipId,
                Guid studentId,
                string summary,
                string? issue,
                string plan,
                DateTime dateReport,
                bool isLate)
            {
                logbookSeeds.Add((
                    internshipId,
                    studentId,
                    summary,
                    issue,
                    plan,
                    NormalizeReportDate(internshipId, dateReport),
                    isLate));
            }

            List<DateTime> BuildWeekStarts(Guid internshipId, int maxWeeks)
            {
                var defaultStart = currentWeekStart.AddDays(-28);
                var start = defaultStart;

                if (internshipWindows.TryGetValue(internshipId, out var window) && window.StartDate.HasValue && window.StartDate.Value > defaultStart)
                {
                    start = GetStartOfWeek(window.StartDate.Value);
                }

                var weekStarts = new List<DateTime>();
                for (var weekStart = start; weekStart <= lastCompletedWeekStart && weekStarts.Count < maxWeeks; weekStart = weekStart.AddDays(7))
                {
                    weekStarts.Add(weekStart);
                }

                if (weekStarts.Count == 0)
                {
                    weekStarts.Add(lastCompletedWeekStart);
                }

                return weekStarts;
            }

            void AddWeekLogs(
                Guid internshipId,
                Guid studentId,
                int weekNumber,
                DateTime weekStart,
                string weekTheme,
                string[] dailyWork,
                bool[] lateFlags)
            {
                var dayNames = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
                var weekTitle = weekNumber switch
                {
                    1 => "Kickoff & Research",
                    2 => "Implementation & Testing",
                    3 => "Optimization & Stabilization",
                    4 => "Release & Handover",
                    _ => "Execution"
                };

                for (var i = 0; i < 5; i++)
                {
                    var isLate = lateFlags.Length > i && lateFlags[i];
                    var badge = isLate ? "Late" : "Submitted";
                    var issue = isLate
                        ? "Late submission due to pending refinement before mentor handoff."
                        : null;

                    AddLog(
                        internshipId,
                        studentId,
                        $"Week {weekNumber}: {weekTitle} - {dayNames[i]}: {dailyWork[i]} (Status Badge: {badge})",
                        issue,
                        $"Next: continue {weekTheme.ToLowerInvariant()} work and keep daily update cadence.",
                        weekStart.AddDays(i),
                        isLate);
                }
            }

            // FPT active group: 4 weeks x Mon-Fri, with intentional Submitted vs Late badges.
            var fptWeekStarts = BuildWeekStarts(proj3InternshipId, 4);

            if (s3 != null)
            {
                var s3Late = new[]
                {
                    new[] { false, false, false, false, false },
                    new[] { false, true,  true, true, true },
                    new[] { false, false, false, true,  false },
                    new[] { false, false, false, false, false }
                };

                var s3Daily = new[]
                {
                    new[]
                    {
                        "Set up environment, dependencies, and onboarding checklist",
                        "Drafted authentication module boundaries",
                        "Implemented login flow with token issuance",
                        "Added unit tests for auth service",
                        "Reviewed PR feedback and refactored middleware"
                    },
                    new[]
                    {
                        "Built internship dashboard API contract",
                        "Implemented role guard matrix for Mentor/UniAdmin",
                        "Added exception mapping for auth endpoints",
                        "Improved API response consistency",
                        "Prepared integration test scenarios"
                    },
                    new[]
                    {
                        "Optimized pagination query for internship listing",
                        "Added cache key normalization for list endpoints",
                        "Implemented notification read-state API",
                        "Hardened validation for update commands",
                        "Documented backend conventions and error catalog"
                    },
                    new[]
                    {
                        "Ran smoke tests for release candidate",
                        "Fixed edge cases from QA report",
                        "Improved audit logging granularity",
                        "Aligned API docs with current response shape",
                        "Prepared demo script and handover notes"
                    }
                };

                for (var w = 0; w < fptWeekStarts.Count && w < 4; w++)
                {
                    AddWeekLogs(
                        proj3InternshipId,
                        s3.StudentId,
                        w + 1,
                        fptWeekStarts[w],
                        "FPT IOC v2 backend delivery",
                        s3Daily[w],
                        s3Late[w]);
                }
            }

            if (s6 != null)
            {
                var s6Week2 = fptWeekStarts.Count >= 2 ? fptWeekStarts[1] : lastCompletedWeekStart;
                var s6Week3 = fptWeekStarts.Count >= 3 ? fptWeekStarts[2] : lastCompletedWeekStart;

                AddWeekLogs(
                    proj3InternshipId,
                    s6.StudentId,
                    2,
                    s6Week2,
                    "API quality assurance",
                    new[]
                    {
                        "Executed API smoke tests for core modules",
                        "Added negative tests for authentication failures",
                        "Validated status filters and sort order behavior",
                        "Checked cache invalidation after create/update",
                        "Consolidated failing test diagnostics"
                    },
                    new[] { false, false, true, false, false });

                AddWeekLogs(
                    proj3InternshipId,
                    s6.StudentId,
                    3,
                    s6Week3,
                    "test hardening and CI stability",
                    new[]
                    {
                        "Triaged flaky tests from CI nightly run",
                        "Improved test fixture isolation",
                        "Parallelized selected integration suites",
                        "Added retry policy for transient db timeouts",
                        "Published test reliability report"
                    },
                    new[] { false, false, false, false, true });
            }

            if (s1 != null)
            {
                var s1Week3 = fptWeekStarts.Count >= 3 ? fptWeekStarts[2] : lastCompletedWeekStart;
                var s1Week4 = fptWeekStarts.Count >= 4 ? fptWeekStarts[3] : lastCompletedWeekStart;

                AddWeekLogs(
                    proj3InternshipId,
                    s1.StudentId,
                    3,
                    s1Week3,
                    "project coordination and risk tracking",
                    new[]
                    {
                        "Prepared sprint board and daily ownership mapping",
                        "Reviewed blockers with mentor and aligned priorities",
                        "Updated requirement traceability matrix",
                        "Consolidated status report for stakeholders",
                        "Closed week with retrospective action items"
                    },
                    new[] { false, false, false, false, false });

                AddWeekLogs(
                    proj3InternshipId,
                    s1.StudentId,
                    4,
                    s1Week4,
                    "demo readiness and release prep",
                    new[]
                    {
                        "Finalized demo flow and environment checklist",
                        "Validated end-to-end happy path",
                        "Collected outstanding bug tickets",
                        "Performed regression pass with QA",
                        "Delivered final weekly summary"
                    },
                    new[] { false, true, false, false, false });
            }

            // Add active Rikkeisoft logs to validate cross-enterprise list/detail scenarios.
            if (rikkeiInternshipId.HasValue)
            {
                var rikkeiWeekStarts = BuildWeekStarts(rikkeiInternshipId.Value, 3);

                if (s7 != null)
                {
                    for (var w = 0; w < rikkeiWeekStarts.Count; w++)
                    {
                        AddWeekLogs(
                            rikkeiInternshipId.Value,
                            s7.StudentId,
                            w + 1,
                            rikkeiWeekStarts[w],
                            "Rikkeisoft internal portal delivery",
                            new[]
                            {
                                "Set up UI shell and role-aware navigation",
                                "Implemented profile module interactions",
                                "Integrated permission matrix with backend",
                                "Reviewed feedback from mentor walkthrough",
                                "Prepared weekly handover notes"
                            },
                            w == 0
                                ? new[] { false, true, true, false, true }
                                : new[] { false, false, true, false, false });
                    }
                }

                if (s4 != null && rikkeiWeekStarts.Count > 0)
                {
                    AddWeekLogs(
                        rikkeiInternshipId.Value,
                        s4.StudentId,
                        1,
                        rikkeiWeekStarts[0],
                        "API contract and auth endpoint alignment",
                        new[]
                        {
                            "Reviewed internal portal API contract",
                            "Mapped permission inheritance edge cases",
                            "Implemented auth endpoint draft",
                            "Added response validation for profile APIs",
                            "Refined DTO naming for FE consistency"
                        },
                        new[] { false, false, false, true, false });
                }
            }

            // Historical group logs: 2 completed workweeks (Mon-Fri) for timeline/history screens.
            if (internshipWindows.TryGetValue(proj5InternshipId, out var historicalWindow) && historicalWindow.EndDate.HasValue)
            {
                var historicalWeek2 = GetStartOfWeek(historicalWindow.EndDate.Value).AddDays(-14);
                var historicalWeek1 = historicalWeek2.AddDays(-7);

                AddWeekLogs(
                    proj5InternshipId,
                    s5.StudentId,
                    1,
                    historicalWeek1,
                    "legacy CRM maintenance",
                    new[]
                    {
                        "Investigated bug clusters in legacy billing module",
                        "Patched null-handling in old service layer",
                        "Verified SQL script backward compatibility",
                        "Validated report generation in staging",
                        "Summarized mitigation notes for maintenance runbook"
                    },
                    new[] { false, false, true, false, false });

                AddWeekLogs(
                    proj5InternshipId,
                    s5.StudentId,
                    2,
                    historicalWeek2,
                    "release hardening and documentation",
                    new[]
                    {
                        "Reviewed unresolved production bug tickets",
                        "Applied fixes for CRM sync scheduler",
                        "Updated deployment rollback checklist",
                        "Ran post-deploy verification tests",
                        "Closed sprint with release report"
                    },
                    new[] { false, false, false, false, true });
            }

            foreach (var seed in logbookSeeds)
            {
                var exists = await _context.Logbooks.AnyAsync(l =>
                    l.InternshipId == seed.InternshipId
                    && l.StudentId == seed.StudentId
                    && l.Summary == seed.Summary
                    && l.DateReport.Date == seed.DateReport.Date
                    && l.DeletedAt == null);

                if (!exists)
                {
                    var logbook = Logbook.Create(
                        seed.InternshipId,
                        seed.StudentId,
                        seed.Summary,
                        seed.Issue,
                        seed.Plan,
                        seed.DateReport);

                    // Control status for UI badge scenarios: Submitted (PUNCTUAL) vs Late (LATE).
                    logbook.CreatedAt = seed.IsLate
                        ? seed.DateReport.Date.AddDays(1).AddHours(9)
                        : seed.DateReport.Date.AddHours(9);
                    logbook.Update(seed.Summary, seed.Issue, seed.Plan, seed.DateReport);

                    _context.Logbooks.Add(logbook);
                }
            }

            await _context.SaveChangesAsync();

            // [NEW] Link WorkItems to Logbooks if not already linked (Using direct SQL for shadow junction table)
            bool alreadyLinked = await _context.Logbooks.AnyAsync(l => l.WorkItems.Any());
            if (!alreadyLinked)
            {
                var workItemsProj3 = await _context.WorkItems.Where(w => w.ProjectId == proj3.ProjectId).ToListAsync();
                var logbooksProj3 = await _context.Logbooks.Where(l => l.InternshipId == proj3InternshipId).ToListAsync();
                
                if (workItemsProj3.Any() && logbooksProj3.Any())
                {
                    foreach (var lb in logbooksProj3)
                    {
                        var wi1 = workItemsProj3[0];
                        if (_context.Database.IsRelational())
                        {
                            await _context.Database.ExecuteSqlRawAsync(
                                "INSERT INTO logbook_work_items (logbook_id, work_items_work_item_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING", 
                                lb.LogbookId, wi1.WorkItemId);
                        }
                        else
                        {
                            if (!lb.WorkItems.Contains(wi1)) lb.WorkItems.Add(wi1);
                        }
                            
                        if (workItemsProj3.Count > 1)
                        {
                            var wi2 = workItemsProj3[1];
                            if (_context.Database.IsRelational())
                            {
                                await _context.Database.ExecuteSqlRawAsync(
                                    "INSERT INTO logbook_work_items (logbook_id, work_items_work_item_id) VALUES ({0}, {1}) ON CONFLICT DO NOTHING", 
                                    lb.LogbookId, wi2.WorkItemId);
                            }
                            else
                            {
                                if (!lb.WorkItems.Contains(wi2)) lb.WorkItems.Add(wi2);
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task SeedStakeholdersAndIssues()
        {
            if (await _context.Stakeholders.AnyAsync()) return;

            var group3 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");

            var customer = new Stakeholder(
                group3.InternshipId,
                "John Doe",
                StakeholderType.Real,
                "john.doe@globaltech.com",
                "Product Owner",
                "Product Owner from Global Tech Corp");
            _context.Stakeholders.Add(customer);

            _context.StakeholderIssues.Add(new StakeholderIssue(
                Guid.NewGuid(),
                customer.Id,
                "Missing Auth Requirements",
                "The current requirement for JWT doesn't specify the token expiration policy."));

            await _context.SaveChangesAsync();
        }

        private async Task SeedEvaluations()
        {
            var phaseFptSpring = await _context.InternshipPhases.FirstOrDefaultAsync(p => p.Name == "FPT Software Spring 2026");
            var phaseRikkeiSpring = await _context.InternshipPhases.FirstOrDefaultAsync(p => p.Name == "Rikkeisoft Spring 2026");
            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            var rikkeiGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");

            var s1 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student1@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s6 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student6@fptu.edu.vn");
            var s4 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s7 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student7@fptu.edu.vn");

            var mentorFpt = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mentor@fptsoftware.com");
            var mentorRikkei = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mentor@rikkeisoft.com");

            if (phaseFptSpring == null || group3 == null || mentorFpt == null) return;

            async Task<EvaluationCycle> EnsureCycleAsync(string cycleName, InternshipPhase phase, DateTime start, DateTime end)
            {
                var cycle = await _context.Set<EvaluationCycle>().FirstOrDefaultAsync(c => c.Name == cycleName);
                if (cycle == null)
                {
                    cycle = new EvaluationCycle
                    {
                        CycleId = Guid.NewGuid(),
                        PhaseId = phase.PhaseId,
                        Name = cycleName,
                        StartDate = start,
                        EndDate = end,
                        Status = EvaluationCycleStatus.Grading
                    };

                    _context.Set<EvaluationCycle>().Add(cycle);
                    await _context.SaveChangesAsync();
                }

                var hasCriteria = await _context.Set<EvaluationCriteria>()
                    .AnyAsync(c => c.CycleId == cycle.CycleId);

                if (!hasCriteria)
                {
                    _context.Set<EvaluationCriteria>().AddRange(
                        new EvaluationCriteria
                        {
                            CriteriaId = Guid.NewGuid(),
                            CycleId = cycle.CycleId,
                            Name = "Technical Skills",
                            Description = "Code quality and architecture",
                            MaxScore = 10,
                            Weight = 0.60m
                        },
                        new EvaluationCriteria
                        {
                            CriteriaId = Guid.NewGuid(),
                            CycleId = cycle.CycleId,
                            Name = "Soft Skills",
                            Description = "Communication and teamwork",
                            MaxScore = 10,
                            Weight = 0.40m
                        });
                    await _context.SaveChangesAsync();
                }

                return cycle;
            }

            async Task EnsurePublishedEvaluationAsync(
                Guid internshipId,
                Guid studentId,
                Guid evaluatorId,
                Guid cycleId,
                decimal technicalScore,
                decimal softSkillScore,
                string note)
            {
                if (await _context.Set<Evaluation>().AnyAsync(e =>
                    e.StudentId == studentId
                    && e.CycleId == cycleId
                    && e.Status == EvaluationStatus.Published
                    && e.DeletedAt == null))
                {
                    return;
                }

                var criteriaList = await _context.Set<EvaluationCriteria>()
                    .Where(c => c.CycleId == cycleId)
                    .OrderByDescending(c => c.Weight)
                    .ToListAsync();

                if (criteriaList.Count >= 2)
                {
                    var technicalCriteria = criteriaList[0];
                    var softSkillCriteria = criteriaList[1];

                    var evalPublished = new Evaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        CycleId = cycleId,
                        InternshipId = internshipId,
                        StudentId = studentId,
                        EvaluatorId = evaluatorId,
                        Status = EvaluationStatus.Published,
                        Note = note
                    };

                    evalPublished.Details.Add(new EvaluationDetail
                    {
                        DetailId = Guid.NewGuid(),
                        EvaluationId = evalPublished.EvaluationId,
                        CriteriaId = technicalCriteria.CriteriaId,
                        Score = technicalScore,
                        Comment = "Strong technical delivery and implementation quality."
                    });

                    evalPublished.Details.Add(new EvaluationDetail
                    {
                        DetailId = Guid.NewGuid(),
                        EvaluationId = evalPublished.EvaluationId,
                        CriteriaId = softSkillCriteria.CriteriaId,
                        Score = softSkillScore,
                        Comment = "Good teamwork, communication, and collaboration."
                    });

                    evalPublished.TotalScore =
                        (technicalScore * technicalCriteria.Weight) +
                        (softSkillScore * softSkillCriteria.Weight);

                    _context.Set<Evaluation>().Add(evalPublished);
                    await _context.SaveChangesAsync();
                }
            }

            var fptCycle = await EnsureCycleAsync(
                "Mid-term Spring 2026",
                phaseFptSpring,
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(10));

            if (s3 != null)
            {
                await EnsurePublishedEvaluationAsync(
                    group3.InternshipId,
                    s3.StudentId,
                    mentorFpt.UserId,
                    fptCycle.CycleId,
                    8.5m,
                    8.0m,
                    "Good progress with stable backend delivery and clear documentation.");
            }

            if (s1 != null)
            {
                await EnsurePublishedEvaluationAsync(
                    group3.InternshipId,
                    s1.StudentId,
                    mentorFpt.UserId,
                    fptCycle.CycleId,
                    8.0m,
                    8.5m,
                    "Consistent coordination, planning discipline, and team support.");
            }

            if (s6 != null)
            {
                await EnsurePublishedEvaluationAsync(
                    group3.InternshipId,
                    s6.StudentId,
                    mentorFpt.UserId,
                    fptCycle.CycleId,
                    7.8m,
                    7.6m,
                    "Testing quality improved; continue strengthening automation depth.");
            }

            if (phaseRikkeiSpring != null && rikkeiGroup != null && mentorRikkei != null)
            {
                var rikkeiCycle = await EnsureCycleAsync(
                    "Mid-term Rikkeisoft Spring 2026",
                    phaseRikkeiSpring,
                    DateTime.UtcNow.AddDays(-8),
                    DateTime.UtcNow.AddDays(12));

                if (s4 != null)
                {
                    await EnsurePublishedEvaluationAsync(
                        rikkeiGroup.InternshipId,
                        s4.StudentId,
                        mentorRikkei.UserId,
                        rikkeiCycle.CycleId,
                        8.2m,
                        7.9m,
                        "Solid API ownership and steady mentoring communication.");
                }

                if (s7 != null)
                {
                    await EnsurePublishedEvaluationAsync(
                        rikkeiGroup.InternshipId,
                        s7.StudentId,
                        mentorRikkei.UserId,
                        rikkeiCycle.CycleId,
                        7.5m,
                        7.8m,
                        "UI delivery is on track; improve punctual update consistency.");
                }
            }

            if (!await _context.Set<Evaluation>().AnyAsync(e =>
                e.InternshipId == group3.InternshipId
                && e.StudentId == null
                && e.CycleId == fptCycle.CycleId
                && e.Status == EvaluationStatus.Published
                && e.DeletedAt == null))
            {
                _context.Set<Evaluation>().Add(new Evaluation
                {
                    EvaluationId = Guid.NewGuid(),
                    CycleId = fptCycle.CycleId,
                    InternshipId = group3.InternshipId,
                    StudentId = null,
                    EvaluatorId = mentorFpt.UserId,
                    Status = EvaluationStatus.Published,
                    Note = "Excellent team coordination overall.",
                    TotalScore = 9.0m
                });
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedProjectResources()
        {
            var proj3 = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "IOC v2.0 Platform");
            if (proj3 != null && !await _context.ProjectResources.AnyAsync(pr => pr.ProjectId == proj3.ProjectId))
            {
                _context.ProjectResources.AddRange(
                    new ProjectResources(proj3.ProjectId, "System Architecture", FileType.PDF, "https://example.com/arch.pdf"),
                    new ProjectResources(proj3.ProjectId, "Figma Design", FileType.LINK, "https://figma.com/design"),
                    new ProjectResources(proj3.ProjectId, "Demo Video", FileType.LINK, "https://youtube.com/demo")
                );
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedViolationReports()
        {
            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            var rikkeiGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");
            var historicalGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");

            var s1 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student1@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s4 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");
            var s7 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student7@fptu.edu.vn");
            var s8 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student8@fptu.edu.vn");
            var s9 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student9@fptu.edu.vn");
            var s10 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student10@fptu.edu.vn");

            var violationSeeds = new List<(Guid StudentId, Guid GroupId, DateOnly OccurredDate, string Description)>();

            if (group3 != null)
            {
                if (s1 != null)
                    violationSeeds.Add((s1.StudentId, group3.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)), "Late check-in without prior notice."));

                if (s3 != null)
                {
                    violationSeeds.Add((s3.StudentId, group3.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), "Student missed the team meeting twice without notice."));
                    violationSeeds.Add((s3.StudentId, group3.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Submitted daily report after cutoff time."));
                    violationSeeds.Add((s3.StudentId, group3.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)), "Skipped mandatory code review session."));
                    violationSeeds.Add((s3.StudentId, group3.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), "Did not update task status before stand-up."));
                }

                if (s8 != null)
                    violationSeeds.Add((s8.StudentId, group3.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), "Pushed changes directly to protected branch without PR."));
            }

            if (rikkeiGroup != null)
            {
                if (s4 != null)
                    violationSeeds.Add((s4.StudentId, rikkeiGroup.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)), "Did not follow pull-request review checklist."));

                if (s7 != null)
                    violationSeeds.Add((s7.StudentId, rikkeiGroup.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), "Absent in daily stand-up without informing mentor."));

                if (s9 != null)
                    violationSeeds.Add((s9.StudentId, rikkeiGroup.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "Missed bug triage session and did not update Jira ticket."));

                if (s10 != null)
                    violationSeeds.Add((s10.StudentId, rikkeiGroup.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)), "Did not submit weekly report before mentor review window."));
            }

            if (historicalGroup != null && s5 != null)
                violationSeeds.Add((s5.StudentId, historicalGroup.InternshipId, DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-4)), "Missed legacy deployment checklist during final sprint."));

            foreach (var seed in violationSeeds)
            {
                var exists = await _context.Set<ViolationReport>().AnyAsync(v =>
                    v.StudentId == seed.StudentId
                    && v.InternshipGroupId == seed.GroupId
                    && v.OccurredDate == seed.OccurredDate
                    && v.Description == seed.Description
                    && v.DeletedAt == null);

                if (exists) continue;

                _context.Set<ViolationReport>().Add(new ViolationReport
                {
                    ViolationReportId = Guid.NewGuid(),
                    StudentId = seed.StudentId,
                    InternshipGroupId = seed.GroupId,
                    OccurredDate = seed.OccurredDate,
                    Description = seed.Description
                });
            }

            await _context.SaveChangesAsync();
        }

         private async Task SeedNotifications()
        {
            var users = await _context.Users.ToListAsync();
            var notifications = new List<Notification>();

            foreach (var user in users)
            {
                if (!await _context.Set<Notification>().AnyAsync(n => n.UserId == user.UserId))
                {
                    if (user.Email == "student3@fptu.edu.vn")
                    {
                        notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = user.UserId, Title = "Hồ sơ thực tập được duyệt", Content = "Hồ sơ thực tập của bạn tại FPT Software đã được duyệt.", Type = NotificationType.ApplicationAccepted, ReferenceType = "InternshipApplication", IsRead = false });
                        notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = user.UserId, Title = "Nhắc nhở nộp báo cáo", Content = "Sắp đến hạn nộp báo cáo định kỳ. Vui lòng cập nhật Logbook.", Type = NotificationType.LogbookFeedback, IsRead = true, ReadAt = DateTime.UtcNow.AddDays(-1) });
                    }
                    else if (user.Email == "student5@fptu.edu.vn")
                    {
                        notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = user.UserId, Title = "Đã nhận được báo cáo Logbook", Content = "Mentor đã nhận xét báo cáo số 1 của bạn.", Type = NotificationType.LogbookFeedback, ReferenceType = "Logbook", IsRead = false });
                        notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = user.UserId, Title = "Kết quả đánh giá", Content = "Đánh giá thực tập của bạn đã được công bố.", Type = NotificationType.EvaluationPublished, IsRead = true, ReadAt = DateTime.UtcNow.AddDays(-2) });
                        notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = user.UserId, Title = "Hoàn thành thực tập", Content = "Chúc mừng bạn đã hoàn thành kỳ thực tập.", Type = NotificationType.General, IsRead = false });
                    }
                    else if (user.Email == "trunguyen.104@gmail.com")
                    {
                        for (int i = 1; i <= 10; i++)
                        {
                            notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = user.UserId, Title = $"Thông báo Demo số {i}", Content = $"Đây là nội dung chi tiết cho thông báo demo số {i}. Vui lòng kiểm tra chức năng hệ thống.", Type = NotificationType.General, IsRead = (i % 3 == 0), ReadAt = (i % 3 == 0) ? DateTime.UtcNow.AddHours(-i) : null });
                        }
                    }
                    else
                    {
                        notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = user.UserId, Title = "Chào mừng đến với hệ thống IOC", Content = $"Kính chào {user.FullName}, đây là thông báo tự động từ hệ thống. Chúc bạn có một ngày làm việc hiệu quả!", Type = NotificationType.General, IsRead = false });
                        notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = user.UserId, Title = "Cập nhật hệ thống", Content = "Hệ thống vừa trải qua một đợt cập nhật các tính năng quan trọng.", Type = NotificationType.General, IsRead = true, ReadAt = DateTime.UtcNow.AddDays(-1) });
                    }
                }
            }

            if (notifications.Any())
            {
                _context.Set<Notification>().AddRange(notifications);
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedSprintWorkItems()
        {
            if (await _context.SprintWorkItems.AnyAsync()) return;

            var sprints = await _context.Sprints.ToListAsync();
            foreach (var sprint in sprints)
            {
                var workItems = await _context.WorkItems
                    .Where(w => w.ProjectId == sprint.ProjectId)
                    .Take(3)
                    .ToListAsync();

                float order = 1.0f;
                foreach (var wi in workItems)
                {
                    _context.SprintWorkItems.Add(new SprintWorkItem 
                    { 
                        SprintId = sprint.SprintId, 
                        WorkItemId = wi.WorkItemId, 
                        BoardOrder = order++ 
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task SeedAuditLogs()
        {
            if (await _context.AuditLogs.AnyAsync()) return;

            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@iocv2.com");
            var mentorFpt = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mentor@fptsoftware.com");

            if (admin != null)
            {
                _context.AuditLogs.AddRange(
                    new AuditLog { AuditLogId = Guid.NewGuid(), Action = AuditAction.Approve, EntityType = "User", EntityId = admin.UserId, PerformedById = admin.UserId, Reason = "Initial admin approval", CreatedAt = DateTime.UtcNow.AddDays(-5) },
                    new AuditLog { AuditLogId = Guid.NewGuid(), Action = AuditAction.Create, EntityType = "University", EntityId = SeedIds.FptuId, PerformedById = admin.UserId, Reason = "Initial setup", CreatedAt = DateTime.UtcNow.AddDays(-10) }
                );
            }

            if (mentorFpt != null)
            {
                var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
                if (group3 != null)
                {
                    _context.AuditLogs.Add(new AuditLog 
                    { 
                        AuditLogId = Guid.NewGuid(), 
                        Action = AuditAction.Update, 
                        EntityType = "InternshipGroup", 
                        EntityId = group3.InternshipId, 
                        PerformedById = mentorFpt.UserId, 
                        Reason = "Update group description", 
                        CreatedAt = DateTime.UtcNow.AddDays(-2) 
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedSecurityTokens()
        {
            if (await _context.RefreshTokens.AnyAsync() || await _context.PasswordResetTokens.AnyAsync()) return;

            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            if (s3 != null)
            {
                _context.RefreshTokens.Add(new RefreshToken 
                { 
                    RefreshTokenId = Guid.NewGuid(), 
                    UserId = s3.UserId, 
                    Token = "mock-refresh-token-student3-2026", 
                    Expires = DateTime.UtcNow.AddDays(7), 
                    CreatedAt = DateTime.UtcNow
                });

                _context.PasswordResetTokens.Add(new PasswordResetToken 
                { 
                    TokenId = Guid.NewGuid(), 
                    UserId = s3.UserId, 
                    TokenHash = "mock-reset-token-s3", 
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(2), 
                    CreatedAt = DateTime.UtcNow 
                });
            }

            await _context.SaveChangesAsync();
        }
    }
}
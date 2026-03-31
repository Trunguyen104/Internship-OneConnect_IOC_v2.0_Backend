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

            // Added HR seed ids for deterministic seeding
            public static readonly Guid HrFptId = new Guid("77777777-7777-7777-7777-777777770001");
            public static readonly Guid HrRikkeisoftId = new Guid("77777777-7777-7777-7777-777777770002");

            // Deterministic EnterpriseUser IDs for mentors — phải cố định để group.MentorId khớp sau mỗi lần seed
            // mentor@fptsoftware.com  → dùng EnterpriseUserId này khi tạo project cho FPT groups
            // mentor@rikkeisoft.com   → dùng EnterpriseUserId này khi tạo project cho Rikkeisoft groups
            public static readonly Guid MentorFptEuId = new Guid("88888888-8888-8888-8888-888888880001");
            public static readonly Guid MentorRikkeisoftEuId = new Guid("88888888-8888-8888-8888-888888880002");

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
        }

        public DbInitializer(AppDbContext context, IPasswordService passwordService, IUserServices userService)
        {
            _context = context;
            _passwordService = passwordService;
            _userService = userService;
        }

        public async Task InitializeAsync()
        {
            // Corrected order to satisfy dependencies:
            // universities -> enterprises -> internship phases -> jobs -> users -> terms -> groups -> projects -> rest...
            await SeedUniversities();
            await SeedEnterprises();
            await SeedInternshipPhases();   // must be before SeedJobs and SeedInternshipGroups
            await SeedJobs();               // depends on InternshipPhases
            await SeedUsers();
            await SeedTerms();
            await SeedInternshipGroups();
            await SeedProjectsAndWorkItems();
            await SeedManageIGProjectData();
            await SeedInternshipStudents();
            await SeedLogbooks();
            await SeedStakeholdersAndIssues();
            await SeedProjectResources();
            await SeedViolationReports();
            await SeedEvaluations();
            await SeedUniAdminTestData();
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

            // Get a valid InternshipPhaseId for each job (required by Job.Create signature)
            var fptPhase = await _context.InternshipPhases.FirstOrDefaultAsync(p => p.EnterpriseId == SeedIds.FptSoftwareId);
            var rikkeiPhase = await _context.InternshipPhases.FirstOrDefaultAsync(p => p.EnterpriseId == SeedIds.RikkeisoftId);

            if (fptPhase == null || rikkeiPhase == null)
                return; // phases not ready

            var job1 = Job.Create(
                SeedIds.FptSoftwareId,
                fptPhase.PhaseId,
                "Junior .NET Intern",
                "Assist backend team building APIs for the IOC v2 platform.",
                "C#, .NET, EF Core, REST, basic SQL",
                "Monthly stipend, mentorship, certificate",
                "Hà Nội (Hybrid)",
                DateTime.UtcNow.AddMonths(2)
            );
            job1.Position = "Backend Intern";
            job1.Status = JobStatus.PUBLISHED;

            var job2 = Job.Create(
                SeedIds.RikkeisoftId,
                rikkeiPhase.PhaseId,
                "Frontend Intern (Angular)",
                "Work on feature improvements and UI polishing for legacy CRM.",
                "Angular, TypeScript, HTML/CSS, basic RxJS",
                "Stipend, mentorship, certificate",
                "Hà Nội (On-site)",
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
                    // Deterministic EnterpriseUserId to keep group.MentorId stable
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

                // HR account
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

            // 4. Students
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
                "student10@fptu.edu.vn"
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
                "Ngô Thị Kiều"
            };

            string[] studentClasses = {
                "SE1616", "SE1617", "SE1616", "SE1618", "SE1617",
                "SE1619", "SE1616", "SE1618", "SE1619", "SE1617"
            };

            string[] studentMajors = {
                "Software Engineering", "Software Engineering", "Information Technology",
                "Software Engineering", "Information Technology", "Software Engineering",
                "Computer Science", "Information Technology", "Software Engineering", "Computer Science"
            };

            for (int i = 0; i < studentEmails.Length; i++)
            {
                if (!existingEmails.Contains(studentEmails[i]))
                {
                    var userId = i < SeedIds.StudentIds.Count ? SeedIds.StudentIds[i] : Guid.NewGuid();
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                    var user = new User(userId, userCode, studentEmails[i], studentNames[i], UserRole.Student, passHash);
                    var gender = i % 2 == 0 ? UserGender.Male : UserGender.Female;
                    user.UpdateProfile(user.FullName, $"098765{phoneCounter++}", null, gender, new DateOnly(2004, 1, 1), "Hà Nội");
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(user.Email);

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

            // Student6 deterministic with CV for job apply tests
            var student6Email = "student6@fptu.edu.vn";
            if (!existingEmails.Contains(student6Email))
            {
                var userId6 = SeedIds.Student6UserId;
                var userCode6 = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                var user6 = new User(userId6, userCode6, student6Email, "Student Six", UserRole.Student, passHash);
                user6.SetStatus(UserStatus.Active);
                _context.Users.Add(user6);
                existingEmails.Add(student6Email);

                var uni6 = universityList.First(u => u.Code == "FPTU");
                _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user6.UserId, UniversityId = uni6.UniversityId });

                var student6 = new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user6.UserId,
                    InternshipStatus = StudentStatus.APPLIED,
                    Major = "Software Engineering",
                    ClassName = "SE1616"
                };

                student6.UpdateCv("https://iocv2-test-resources.s3.amazonaws.com/resumes/student6_cv.pdf");

                _context.Students.Add(student6);
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

            var allStudents = await _context.Students
                .Include(s => s.User)
                .Where(s => s.User.Email.StartsWith("student") && s.User.Email.EndsWith("@fptu.edu.vn"))
                .ToListAsync();

            // spring2026 is created above if missing, use null-forgiving to satisfy analyzer
            foreach (var student in allStudents)
            {
                if (!await _context.StudentTerms.AnyAsync(st => st.StudentId == student.StudentId && st.TermId == spring2026!.TermId))
                {
                    _context.StudentTerms.Add(new StudentTerm
                    {
                        StudentTermId = Guid.NewGuid(),
                        StudentId = student.StudentId,
                        TermId = spring2026!.TermId,
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
            var fsoft = await _context.Enterprises.FirstOrDefaultAsync(e => e.Name == "FPT Software");
            var rikkeisoft = await _context.Enterprises.FirstOrDefaultAsync(e => e.Name == "Rikkeisoft");
            if (fsoft == null || rikkeisoft == null) return;

            async Task EnsurePhase(
                Guid enterpriseId, string name,
                DateOnly start, DateOnly end,
                string majorFields, int capacity, string? description,
                InternshipPhaseStatus targetStatus)
            {
                if (await _context.InternshipPhases
                        .AnyAsync(p => p.EnterpriseId == enterpriseId && p.Name == name))
                    return;

                var phase = InternshipPhase.Create(enterpriseId, name, start, end, majorFields, capacity, description);

                if (targetStatus != InternshipPhaseStatus.Draft)
                    phase.UpdateInfo(name, start, end, majorFields, capacity, description, targetStatus);

                _context.InternshipPhases.Add(phase);
            }

            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Fall 2025",
                new DateOnly(2025, 9, 1), new DateOnly(2025, 12, 31),
                "Software Engineering, Information Technology", 30,
                "Đợt thực tập Fall 2025 của FPT Software — đã kết thúc",
                InternshipPhaseStatus.Closed);

            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Spring 2026",
                new DateOnly(2026, 1, 15), new DateOnly(2026, 4, 30),
                "Software Engineering, Information Technology, Computer Science", 50,
                "Đợt thực tập Spring 2026 của FPT Software — đang diễn ra",
                InternshipPhaseStatus.InProgress);

            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Summer 2026",
                new DateOnly(2026, 5, 1), new DateOnly(2026, 8, 31),
                "Software Engineering, Data Engineering", 40,
                "Đợt thực tập Summer 2026 của FPT Software — đang tuyển",
                InternshipPhaseStatus.Open);

            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Fall 2025",
                new DateOnly(2025, 9, 1), new DateOnly(2025, 12, 31),
                "Software Engineering, Information Technology", 20,
                "Đợt thực tập Fall 2025 của Rikkeisoft — đã kết thúc",
                InternshipPhaseStatus.Closed);

            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Spring 2026",
                new DateOnly(2026, 2, 1), new DateOnly(2026, 5, 31),
                "Software Engineering, Computer Science", 20,
                "Đợt thực tập Spring 2026 của Rikkeisoft — đang diễn ra",
                InternshipPhaseStatus.InProgress);

            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Summer 2026",
                new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 30),
                "Software Engineering", 15,
                "t thc tp Summer 2026 ca Rikkeisoft €” bn nhp",
                InternshipPhaseStatus.Draft);

            // HAPPY CASES
            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Autumn 2026",
                new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31),
                "Software Engineering, Cyber Security", 60,
                "Đợt thực tập Mùa Thu 2026 - Tương lai xán lạn",
                InternshipPhaseStatus.Draft);

            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Autumn 2026",
                new DateOnly(2026, 9, 1), new DateOnly(2026, 12, 31),
                "Artificial Intelligence, Software Engineering", 25,
                "Đợt thực tập Mùa Thu 2026 - Cơ hội mới",
                InternshipPhaseStatus.Open);

            // UNHAPPY/EDGE CASES
            
            // 1. Minimum capacity (1) and very short duration (1 week)
            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Micro Internship Express",
                new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 7),
                "Software Engineering", 1,
                "Chương trình thực tập siêu tốc 1 tuần cho 1 sinh viên xuất sắc nhất.",
                InternshipPhaseStatus.Draft);

            // 2. Maximum capacity and very long duration (1 year)
            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Mega-Internship 2027",
                new DateOnly(2027, 1, 1), new DateOnly(2027, 12, 31),
                "All IT Majors, Business, Design, HR, Marketing", 9999,
                "Đợt thực tập lớn nhất lịch sử với 9999 sinh viên trong suốt 1 năm tròn",
                InternshipPhaseStatus.Draft);

            // 3. Past Start Date and End Date but accidentally status is Open (Simulating forgotten close edge case)
            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Forgotten Phase 2024",
                new DateOnly(2024, 1, 1), new DateOnly(2024, 6, 30),
                "Software Engineering", 10,
                "Đã qua rất lâu nhưng trạng thái chưa được update thành Closed",
                InternshipPhaseStatus.Open);

            // 4. End Date is same as Start Date (1 day duration)
            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft One Day Challenge",
                new DateOnly(2026, 11, 11), new DateOnly(2026, 11, 11),
                "Data Science", 5,
                "Thực tập trong vỏn vẹn 1 ngày duy nhất!",
                InternshipPhaseStatus.Closed);

            // 5. Very long Major fields string and description
            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Specialized Tech Hub 2026",
                new DateOnly(2026, 8, 1), new DateOnly(2026, 11, 30),
                "Software Engineering, Information Technology, Computer Science, Data Engineering, Cyber Security, Artificial Intelligence, Machine Learning, Cloud Computing, DevOps, Blockchain, Internet of Things (IoT), Robotics, Embedded Systems, Game Development, UI/UX Design", 100,
                "Đợt thực tập quy tụ tất cả các chuyên ngành từ cơ bản đến nâng cao. " +
                "Sinh viên sẽ được làm việc trong các lab nghiên cứu sâu về các công nghệ xu hướng mới nhất. " +
                "Yêu cầu: Sinh viên có GPA cao, tiếng Anh tốt và đam mê mãnh liệt với công nghệ.",
                InternshipPhaseStatus.InProgress);

            await _context.SaveChangesAsync();
        }

        private async Task SeedInternshipGroups()
        {
            var fsoft = await _context.Enterprises.FirstOrDefaultAsync(e => e.Name == "FPT Software");
            var rikkeisoft = await _context.Enterprises.FirstOrDefaultAsync(e => e.Name == "Rikkeisoft");
            if (fsoft == null || rikkeisoft == null) return;

            var mentorFptEuId = SeedIds.MentorFptEuId;
            var mentorRikkeisEuId = SeedIds.MentorRikkeisoftEuId;

            var phaseInProgressFpt = await _context.InternshipPhases.FirstOrDefaultAsync(
                p => p.EnterpriseId == fsoft.EnterpriseId && p.Name == "FPT Software Spring 2026");
            var phaseClosedFpt = await _context.InternshipPhases.FirstOrDefaultAsync(
                p => p.EnterpriseId == fsoft.EnterpriseId && p.Name == "FPT Software Fall 2025");
            var phaseInProgressRikkei = await _context.InternshipPhases.FirstOrDefaultAsync(
                p => p.EnterpriseId == rikkeisoft.EnterpriseId && p.Name == "Rikkeisoft Spring 2026");
            var phaseClosedRikkei = await _context.InternshipPhases.FirstOrDefaultAsync(
                p => p.EnterpriseId == rikkeisoft.EnterpriseId && p.Name == "Rikkeisoft Fall 2025");

            if (phaseInProgressFpt == null || phaseClosedFpt == null || phaseInProgressRikkei == null || phaseClosedRikkei == null)
                return;

            var spring2026 = await _context.Terms.FirstOrDefaultAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");
            var fall2025 = await _context.Terms.FirstOrDefaultAsync(t => t.Name == "Fall 2025" && t.University.Code == "FPTU");
            var spring2026Ct = await _context.Terms.Include(t => t.University).FirstOrDefaultAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU-CT");

            // If these essential terms are missing, skip application seeding to avoid null dereference
            if (spring2026 == null || fall2025 == null)
            {
                await _context.SaveChangesAsync();
                return;
            }

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

            await _context.SaveChangesAsync();

            // Seed some applications (guarding jobs exist)
            var fptJob = await _context.Jobs.FirstOrDefaultAsync(j => j.EnterpriseId == SeedIds.FptSoftwareId);
            var rikkeiJob = await _context.Jobs.FirstOrDefaultAsync(j => j.EnterpriseId == SeedIds.RikkeisoftId);

            var s1 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student1@fptu.edu.vn");
            var s2 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s4 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");

            if (fptJob != null && rikkeiJob != null && s2 != null && s3 != null)
            {
                if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.StudentId == s3.StudentId && a.TermId == spring2026.TermId))
                    _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s3.StudentId, JobId = fptJob.JobId, Status = InternshipApplicationStatus.Placed, AppliedAt = DateTime.UtcNow.AddDays(-40) });

                if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == rikkeisoft.EnterpriseId && a.StudentId == s2.StudentId && a.TermId == spring2026.TermId))
                    _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s2.StudentId, JobId = rikkeiJob.JobId, Status = InternshipApplicationStatus.PendingAssignment, AppliedAt = DateTime.UtcNow.AddDays(-10) });
            }

            if (rikkeiJob != null && s4 != null && !await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == rikkeisoft.EnterpriseId && a.StudentId == s4.StudentId && a.TermId == spring2026.TermId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s4.StudentId, Status = InternshipApplicationStatus.Applied, AppliedAt = DateTime.UtcNow.AddDays(-2) });
            }

            if (fptJob != null && s2 != null && !await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.StudentId == s2.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = fall2025.TermId, StudentId = s2.StudentId, Status = InternshipApplicationStatus.Rejected, RejectReason = "Not a good fit for this semester", AppliedAt = DateTime.UtcNow.AddDays(-100) });
            }

            await _context.SaveChangesAsync();

            // Assign Approved/Placed applications deterministically
            var allStudents = await _context.Students
                .Include(s => s.User)
                .Where(s => s.User.Email.StartsWith("student") && s.User.Email.EndsWith("@fptu.edu.vn"))
                .ToListAsync();

            var orderedStudents = allStudents
                .OrderBy(s => int.Parse(System.Text.RegularExpressions.Regex.Match(s.User.Email, @"\d+").Value))
                .ToList();

            var fstudents = new[] { 0, 1, 2, 5, 6, 7 };
            var rstudents = new[] { 3, 4, 8, 9 };

            foreach (var idx in fstudents)
            {
                if (idx >= orderedStudents.Count) continue;
                var stu = orderedStudents[idx];
                if (!await _context.InternshipApplications.AnyAsync(
                    a => a.EnterpriseId == fsoft.EnterpriseId
                      && a.TermId == spring2026!.TermId
                      && a.StudentId == stu.StudentId))
                {
                    _context.InternshipApplications.Add(new InternshipApplication
                    {
                        ApplicationId = Guid.NewGuid(),
                        EnterpriseId = fsoft.EnterpriseId,
                        TermId = spring2026!.TermId,
                        StudentId = stu.StudentId,
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
                        Status = InternshipApplicationStatus.Placed,
                        AppliedAt = DateTime.UtcNow.AddDays(-25)
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedProjectsAndWorkItems()
        {
            if (await _context.Projects.AnyAsync()) return;

            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            var group5 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
            if (group3 == null || group5 == null) return;

            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");

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
            if (s3 != null)
            {
                _context.WorkItems.AddRange(
                    new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3.ProjectId, Title = "Design DB Schema", Type = WorkItemType.Task, Status = WorkItemStatus.Done, AssigneeId = s3.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)) },
                    new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3.ProjectId, Title = "Implement JWT", Type = WorkItemType.Task, Status = WorkItemStatus.InProgress, AssigneeId = s3.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)) },
                    new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3.ProjectId, Title = "Unit Testing Auth", Type = WorkItemType.Task, Status = WorkItemStatus.Todo, AssigneeId = s3.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)) }
                );
            }

            // Work Items for S5
            if (s5 != null)
            {
                for (int i = 1; i <= 5; i++)
                {
                    _context.WorkItems.Add(new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj5.ProjectId, Title = $"Legacy fix #{i}", Type = WorkItemType.Task, Status = WorkItemStatus.Done, AssigneeId = s5.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-3).AddDays(i * 10)) });
                }
            }

            // Pending project example
            var projPending = Project.Create("FPT Future System", "Next phase architecture", "PRJ-FPTSOF_FPT_2", "CNTT", "Design next phase architecture.", mentorId: SeedIds.MentorFptEuId);
            projPending.AssignToGroup(group3.InternshipId, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(30));
            if (!await _context.Projects.AnyAsync(p => p.ProjectName == "FPT Future System"))
            {
                _context.Projects.Add(projPending);

                var cancelledSprint = new Sprint(projPending.ProjectId, "Cancelled Sprint", "A sprint that was planned but cancelled");
                cancelledSprint.Start(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));
                _context.Sprints.Add(cancelledSprint);

                _context.WorkItems.AddRange(
                    new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = projPending.ProjectId, Title = "Gather Requirements", Type = WorkItemType.Task, Status = WorkItemStatus.Cancelled, AssigneeId = null, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)) },
                    new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = projPending.ProjectId, Title = "Initial Design", Type = WorkItemType.Task, Status = WorkItemStatus.Todo, AssigneeId = null, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)) }
                );

                await _context.SaveChangesAsync();

                var requirementsTask = await _context.WorkItems.FirstOrDefaultAsync(w => w.Title == "Gather Requirements" && w.ProjectId == projPending.ProjectId);
                if (requirementsTask != null)
                {
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
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedManageIGProjectData()
        {
            var rikkeiGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");
            if (rikkeiGroup != null && !await _context.Projects.AnyAsync(p => p.InternshipId == rikkeiGroup.InternshipId))
            {
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

            if (!await _context.Projects.AnyAsync(p => p.ProjectName == "Orphan Research Project"))
            {
                var orphanProj = Project.Create(
                    "Orphan Research Project",
                    "Dự án nghiên cứu bị orphan do nhóm thực tập đã bị xóa",
                    "PRJ-ORPHAN_001",
                    "Nghiên cứu",
                    "Nghiên cứu ứng dụng AI trong kiểm thử phần mềm.",
                    mentorId: SeedIds.MentorFptEuId);
                orphanProj.SetOrphan();
                orphanProj.Publish();
                _context.Projects.Add(orphanProj);
            }

            if (!await _context.Projects.AnyAsync(p => p.ProjectName == "FPT AI Code Review Tool"))
            {
                var draftUnassigned = Project.Create(
                    "FPT AI Code Review Tool",
                    "Công cụ review code tự động sử dụng AI cho intern FPT Software",
                    "PRJ-FPTSOF_FPT_5",
                    "CNTT",
                    "Xây dựng tool phân tích code, phát hiện bug và gợi ý cải thiện bằng LLM.",
                    mentorId: SeedIds.MentorFptEuId);
                _context.Projects.Add(draftUnassigned);
            }

            await _context.SaveChangesAsync();
        }

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
            var s4 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");

            // FPT Software OJT Team Alpha: s1(Leader), s2(Member), s3(Member), s6(Member)
            bool fptHasStudents = await _context.InternshipStudents.AnyAsync(m => m.InternshipId == fptGroup.InternshipId);
            if (!fptHasStudents)
            {
                if (s1 != null) fptGroup.AddMember(s1.StudentId, InternshipRole.Leader);
                if (s2 != null) fptGroup.AddMember(s2.StudentId, InternshipRole.Member);
                if (s3 != null) fptGroup.AddMember(s3.StudentId, InternshipRole.Member);
                if (s6 != null) fptGroup.AddMember(s6.StudentId, InternshipRole.Member);
                _context.InternshipGroups.Update(fptGroup);
            }

            // Rikkeisoft Spring 2026 Team: s4(Leader), s5(Member), s7(Member)
            bool rikkeiHasStudents = await _context.InternshipStudents.AnyAsync(m => m.InternshipId == rikkeiGroup.InternshipId);
            if (!rikkeiHasStudents)
            {
                if (s4 != null) rikkeiGroup.AddMember(s4.StudentId, InternshipRole.Leader);
                if (s5 != null) rikkeiGroup.AddMember(s5.StudentId, InternshipRole.Member);
                if (s7 != null) rikkeiGroup.AddMember(s7.StudentId, InternshipRole.Member);
                _context.InternshipGroups.Update(rikkeiGroup);
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedLogbooks()
        {
            var proj3 = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "IOC v2.0 Platform");
            var proj5 = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "Legacy CRM Maintenance");
            var s1 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student1@fptu.edu.vn");
            var s2 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");

            if (proj3 == null || proj5 == null || s3 == null || s5 == null || !proj3.InternshipId.HasValue || !proj5.InternshipId.HasValue) return;

            var proj3InternshipId = proj3.InternshipId.Value;
            var proj5InternshipId = proj5.InternshipId.Value;

            if (!await _context.Logbooks.AnyAsync())
            {
                _context.Logbooks.AddRange(
                    Logbook.Create(proj3InternshipId, s3.StudentId, "Integrated basic project structure.", null, "Focus on Auth module.", DateTime.UtcNow.AddDays(-7)),
                    Logbook.Create(proj3InternshipId, s3.StudentId, "Started JWT implementation.", "Encountered some middleware issues.", "Resolve middleware and test login.", DateTime.UtcNow.AddDays(-1))
                );

                if (s1 != null) _context.Logbooks.Add(Logbook.Create(proj3InternshipId, s1.StudentId, "Initial requirement analysis and documentation.", null, "Finalize SRS.", DateTime.UtcNow.AddDays(-6)));
                if (s2 != null) _context.Logbooks.Add(Logbook.Create(proj3InternshipId, s2.StudentId, "UI/UX wireframing for main dashboard.", "Feedback from PO required.", "Update Figma design.", DateTime.UtcNow.AddDays(-5)));

                for (int i = 1; i <= 4; i++)
                {
                    _context.Logbooks.Add(Logbook.Create(proj5InternshipId, s5.StudentId, $"Work report {i}", null, "Continue next task", DateTime.UtcNow.AddMonths(-6 + i)));
                }
                
                await _context.SaveChangesAsync();
            }

            // Link WorkItems to Logbooks using navigation properties (portable across DB providers)
            bool alreadyLinked = await _context.Logbooks.AnyAsync(l => l.WorkItems.Any());
            if (!alreadyLinked)
            {
                var workItemsProj3 = await _context.WorkItems.Where(w => w.ProjectId == proj3.ProjectId).ToListAsync();
                var logbooksProj3 = await _context.Logbooks.Where(l => l.InternshipId == proj3InternshipId).Include(l => l.WorkItems).ToListAsync();

                if (workItemsProj3.Any() && logbooksProj3.Any())
                {
                    foreach (var lb in logbooksProj3)
                    {
                        foreach (var wi in workItemsProj3.Take(2))
                        {
                            if (!lb.WorkItems.Any(x => x.WorkItemId == wi.WorkItemId))
                            {
                                lb.WorkItems.Add(wi);
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

            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");

            if (group3 == null || s3 == null) return;

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
            // Use the actual seeded group name
            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var mentorFpt = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mentor@fptsoftware.com");

            if (phaseFptSpring == null || group3 == null || s3 == null || mentorFpt == null) return;

            var cycle = await _context.Set<EvaluationCycle>().FirstOrDefaultAsync(c => c.Name == "Mid-term Spring 2026");
            if (cycle == null)
            {
                cycle = new EvaluationCycle
                {
                    CycleId = Guid.NewGuid(),
                    PhaseId = phaseFptSpring.PhaseId,
                    Name = "Mid-term Spring 2026",
                    StartDate = DateTime.UtcNow.AddDays(-10),
                    EndDate = DateTime.UtcNow.AddDays(10),
                    Status = EvaluationCycleStatus.Grading
                };

                cycle.Criteria.Add(new EvaluationCriteria { CriteriaId = Guid.NewGuid(), CycleId = cycle.CycleId, Name = "Technical Skills", Description = "Code quality and architecture", Weight = 0.60m });
                cycle.Criteria.Add(new EvaluationCriteria { CriteriaId = Guid.NewGuid(), CycleId = cycle.CycleId, Name = "Soft Skills", Description = "Communication and teamwork", Weight = 0.40m });

                _context.Set<EvaluationCycle>().Add(cycle);
                await _context.SaveChangesAsync();
            }

            if (!await _context.Set<Evaluation>().AnyAsync(e => e.StudentId == s3.StudentId && e.CycleId == cycle.CycleId))
            {
                var criteriaList = cycle.Criteria.ToList();
                if (criteriaList.Count >= 2)
                {
                    var evalDraft = new Evaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        CycleId = cycle.CycleId,
                        InternshipId = group3.InternshipId,
                        StudentId = s3.StudentId,
                        EvaluatorId = mentorFpt.UserId,
                        Status = EvaluationStatus.Draft,
                        Note = "Good progress, but needs better test coverage."
                    };

                    evalDraft.Details.Add(new EvaluationDetail { DetailId = Guid.NewGuid(), EvaluationId = evalDraft.EvaluationId, CriteriaId = criteriaList[0].CriteriaId, Score = 8.0m, Comment = "Solid coding" });
                    evalDraft.Details.Add(new EvaluationDetail { DetailId = Guid.NewGuid(), EvaluationId = evalDraft.EvaluationId, CriteriaId = criteriaList[1].CriteriaId, Score = 7.5m, Comment = "Good speaker" });
                    evalDraft.TotalScore = (8.0m * 0.60m) + (7.5m * 0.40m);

                    _context.Set<Evaluation>().Add(evalDraft);

                    var evalPublishedGroup = new Evaluation
                    {
                        EvaluationId = Guid.NewGuid(),
                        CycleId = cycle.CycleId,
                        InternshipId = group3.InternshipId,
                        StudentId = null,
                        EvaluatorId = mentorFpt.UserId,
                        Status = EvaluationStatus.Published,
                        Note = "Excellent team coordination overall.",
                        TotalScore = 9.0m
                    };
                    _context.Set<Evaluation>().Add(evalPublishedGroup);

                    await _context.SaveChangesAsync();
                }
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
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");

            if (group3 != null && s3 != null && !await _context.Set<ViolationReport>().AnyAsync(v => v.StudentId == s3.StudentId))
            {
                _context.Set<ViolationReport>().Add(new ViolationReport
                {
                    ViolationReportId = Guid.NewGuid(),
                    StudentId = s3.StudentId,
                    InternshipGroupId = group3.InternshipId,
                    OccurredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
                    Description = "Student missed the team meeting twice without notice."
                });
            }
            await _context.SaveChangesAsync();
        }

         /// <summary>
        /// Seeds detailed test data for feat/uni-admin-monitor-internship-activities.
        /// Covers happy / unhappy / edge cases for logbook-total, logbook-weekly, evaluations.
        ///
        /// Students in FPT Alpha (s1, s2, s3, s6) and Rikkei Spring (s4, s5, s7):
        ///   s1 – Sufficient  (≥75 %)  : 17 logbooks / ~20 required days
        ///   s2 – SlightlyMissing (50–75%) : 12 logbooks
        ///   s3 – MissingMany (<50%)    :  8 logbooks + violations
        ///   s6 – Edge: just joined today, 0 logbooks
        ///   s4 – Sufficient  (≥75%)    : 11 logbooks / ~13 required days
        ///   s5 – MissingMany (<50%)    :  3 logbooks (Rikkei active group)
        ///   s7 – Edge: just joined today, 0 logbooks
        ///
        /// Evaluations (Published, visible to UniAdmin):
        ///   FPT Mid-term cycle  : s1 (8.8), s3 (7.7) — Published
        ///   FPT Final cycle     : s1 (9.3), s3 (7.6) — Published
        ///   Rikkei Mid-term     : s4 (8.3)            — Published
        ///   s2 — only group evaluation (studentId=null), returns empty for individual
        ///   s5 — Draft only, not visible
        /// </summary>
        private async Task SeedUniAdminTestData()
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
            var s4 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");
            var s6 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student6@fptu.edu.vn");
            var s7 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student7@fptu.edu.vn");
            var mentorFpt = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mentor@fptsoftware.com");
            var mentorRikkei = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mentor@rikkeisoft.com");

            if (s1 == null || s2 == null || s3 == null || s4 == null || s5 == null || s6 == null || s7 == null
                || mentorFpt == null || mentorRikkei == null) return;

            var spring2026 = await _context.Terms
                .Include(t => t.University)
                .FirstOrDefaultAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");
            if (spring2026 == null) return;

            // ── 1. Set StudentTerm.EnterpriseId so logbook/evaluation handlers can find internship groups ──
            var fptStudentIds    = new[] { s1.StudentId, s2.StudentId, s3.StudentId, s6.StudentId };
            var rikkeiStudentIds = new[] { s4.StudentId, s5.StudentId, s7.StudentId };

            var termsToUpdate = await _context.StudentTerms
                .Where(st => st.TermId == spring2026.TermId
                          && (fptStudentIds.Contains(st.StudentId) || rikkeiStudentIds.Contains(st.StudentId))
                          && st.EnterpriseId == null)
                .ToListAsync();

            foreach (var st in termsToUpdate)
            {
                st.EnterpriseId = fptStudentIds.Contains(st.StudentId)
                    ? SeedIds.FptSoftwareId
                    : SeedIds.RikkeisoftId;
            }
            await _context.SaveChangesAsync();

            // ── 2. Update InternshipStudent.JoinedAt for realistic logbook periods ──
            //   FPT Alpha: s1/s2/s3 joined 28 days ago; s6 joined today (edge: very recent joiner)
            //   Rikkei:    s4/s5   joined 18 days ago; s7 joined today
            var fptJoinedAt    = DateTime.UtcNow.AddDays(-28);
            var rikkeiJoinedAt = DateTime.UtcNow.AddDays(-18);

            var fptMembers = await _context.InternshipStudents
                .Where(m => m.InternshipId == fptGroup.InternshipId)
                .ToListAsync();
            var rikkeiMembers = await _context.InternshipStudents
                .Where(m => m.InternshipId == rikkeiGroup.InternshipId)
                .ToListAsync();

            foreach (var m in fptMembers)
            {
                if (m.StudentId == s1.StudentId || m.StudentId == s2.StudentId || m.StudentId == s3.StudentId)
                    m.JoinedAt = fptJoinedAt;
                // s6 keeps JoinedAt = UtcNow (just joined today — edge case)
            }
            foreach (var m in rikkeiMembers)
            {
                if (m.StudentId == s4.StudentId || m.StudentId == s5.StudentId)
                    m.JoinedAt = rikkeiJoinedAt;
                // s7 keeps JoinedAt = UtcNow (just joined today)
            }
            await _context.SaveChangesAsync();

            // ── 3. Detailed logbooks ──
            // Guard: skip if s1 already has detailed logbooks in fptGroup
            bool alreadyDetailed = await _context.Logbooks.CountAsync(
                l => l.InternshipId == fptGroup.InternshipId && l.StudentId == s1.StudentId) >= 5;
            if (!alreadyDetailed)
            {
                // Helper: create logbook with a DateReport N days ago (Status=LATE since CreatedAt=now)
                // DateReport = today  → PUNCTUAL (same date as CreatedAt at seed time)
                // DateReport = past   → LATE

                var fptId    = fptGroup.InternshipId;
                var rikkeiId = rikkeiGroup.InternshipId;

                // ── s1 (Nguyễn Văn An) — HIGH COMPLETION ~81%, Leader FPT ──
                // 16 LATE (past days) + 1 PUNCTUAL (today) = 17 submitted / ~21 required
                var s1Logbooks = new[]
                {
                    (-27, s1.StudentId, fptId, "Kickoff: phân tích yêu cầu hệ thống và lên kế hoạch sprint.",                     null,                                  "Hoàn thiện SRS draft."),
                    (-26, s1.StudentId, fptId, "Thiết kế kiến trúc microservice và xác định bounded contexts.",                    null,                                  "Vẽ diagram C4 Model."),
                    (-25, s1.StudentId, fptId, "Setup môi trường CI/CD pipeline với GitHub Actions.",                              "Pipeline test đang chậm.",             "Tối ưu cache Docker layer."),
                    (-24, s1.StudentId, fptId, "Xây dựng base project .NET 8 với Clean Architecture.",                            null,                                  "Implement repository pattern."),
                    (-21, s1.StudentId, fptId, "Cài đặt EF Core + migrations ban đầu.",                                          "Conflict migration khi merge.",        "Fix migration conflict và viết integration test."),
                    (-20, s1.StudentId, fptId, "Implement Identity module: register, login, JWT.",                                null,                                  "Thêm refresh token và logout."),
                    (-19, s1.StudentId, fptId, "Viết unit test cho AuthService — coverage 85%.",                                  "Một số mock khó setup.",              "Refactor để dễ test hơn."),
                    (-18, s1.StudentId, fptId, "Code review PR của s3, đề xuất cải tiến xử lý exception.",                       null,                                  "Merge PR sau khi s3 fix."),
                    (-17, s1.StudentId, fptId, "Implement API endpoint CRUD cho Student module.",                                 null,                                  "Viết Swagger docs và integration test."),
                    (-14, s1.StudentId, fptId, "Fix bug pagination trả về sai totalPages.",                                      "Lỗi do off-by-one trong query.",      "Thêm edge-case test."),
                    (-13, s1.StudentId, fptId, "Tối ưu query N+1 bằng eager loading Include().",                                 null,                                  "Benchmark trước và sau tối ưu."),
                    (-12, s1.StudentId, fptId, "Implement file upload lên S3 cho CV sinh viên.",                                  "Cần config CORS bucket policy.",      "Test với nhiều loại file."),
                    (-11, s1.StudentId, fptId, "Chuẩn bị báo cáo mid-term, review toàn bộ tính năng đã làm.",                   null,                                  "Demo với mentor vào ngày mai."),
                    (-10, s1.StudentId, fptId, "Demo mid-term với mentor — nhận phản hồi cải thiện UI/UX.",                      null,                                  "Triển khai theo góp ý của mentor."),
                    (-7,  s1.StudentId, fptId, "Implement notification module — SignalR real-time alerts.",                      "SignalR hub cần authenticate.",       "Thêm JWT middleware cho hub."),
                    (-4,  s1.StudentId, fptId, "Viết E2E test cho luồng apply internship.",                                      null,                                  "Hoàn thành toàn bộ test suite."),
                    (0,   s1.StudentId, fptId, "Review tổng thể code và chuẩn bị deploy staging environment.",                   null,                                  "Deploy và smoke test."),
                };

                // ── s2 (Trần Thị Bình) — MEDIUM COMPLETION ~57%, Member FPT ──
                // 12 submitted / ~21 required  →  SlightlyMissing
                var s2Logbooks = new[]
                {
                    (-27, s2.StudentId, fptId, "Nghiên cứu Figma design system và bắt đầu wireframe.",                           null,                                  "Hoàn thiện wireframe màn hình dashboard."),
                    (-26, s2.StudentId, fptId, "Thiết kế UI/UX cho màn hình danh sách sinh viên.",                               "Mentor chưa approve design.",         "Chỉnh sửa theo feedback."),
                    (-24, s2.StudentId, fptId, "Code React component cho StudentListPage.",                                      null,                                  "Kết nối API và xử lý loading state."),
                    (-21, s2.StudentId, fptId, "Implement filter và search trên StudentList.",                                   "API filter chưa đồng bộ với FE.",     "Làm việc với s1 để align API contract."),
                    (-19, s2.StudentId, fptId, "Xây dựng StudentDetailPage với tab navigation.",                                 null,                                  "Thêm tab Logbook và Evaluation."),
                    (-14, s2.StudentId, fptId, "Fix responsive layout trên màn hình mobile.",                                   "Breakpoint 375px bị vỡ layout.",      "Test trên nhiều thiết bị."),
                    (-13, s2.StudentId, fptId, "Tích hợp Chart.js vẽ biểu đồ logbook completion.",                              null,                                  "Thêm tooltip và legend."),
                    (-10, s2.StudentId, fptId, "Demo UI mid-term với mentor — nhận feedback về accessibility.",                  null,                                  "Thêm aria-label và keyboard navigation."),
                    (-7,  s2.StudentId, fptId, "Implement WeeklyLogbook component — dạng calendar view.",                       "Dữ liệu tuần hiện tại bị sai timezone.", "Debug UTC vs local timezone."),
                    (-4,  s2.StudentId, fptId, "Refactor component structure — tách BusinessLogic ra hook.",                    null,                                  "Viết storybook docs."),
                    (-3,  s2.StudentId, fptId, "Tích hợp EvaluationPanel hiển thị điểm đánh giá.",                              "API response format khác expected.", "Align với s1 về DTO structure."),
                    (0,   s2.StudentId, fptId, "Kiểm thử toàn bộ luồng từ student login đến view logbook.",                     null,                                  "Viết báo cáo test cases."),
                };

                // ── s3 (Lê Văn Cường) — LOW COMPLETION ~38%, Member FPT — plus violations ──
                // 2 existing (days -7, -1) + 6 new = 8 total submitted / ~21 required → MissingMany
                // Add 6 new logbooks for days not yet seeded
                var s3ExistingDates = await _context.Logbooks
                    .Where(l => l.InternshipId == fptId && l.StudentId == s3.StudentId)
                    .Select(l => l.DateReport.Date)
                    .ToListAsync();

                var s3NewLogbooks = new[]
                {
                    (-25, s3.StudentId, fptId, "Bắt đầu task Implement JWT refresh token.",                                      "Chưa hiểu rõ flow token rotation.",   "Đọc RFC 6819 và trao đổi với s1."),
                    (-20, s3.StudentId, fptId, "Implement middleware xử lý exception toàn cục.",                                 null,                                  "Viết unit test cho middleware."),
                    (-15, s3.StudentId, fptId, "Debug lỗi 500 Internal Server Error khi upload file lớn.",                      "File > 10MB bị timeout.",             "Config request size limit và test lại."),
                    (-10, s3.StudentId, fptId, "Fix lỗi migration conflict sau khi merge nhánh feature.",                       "EF Core migration xung đột.",         "Xóa migration cũ và re-generate."),
                    (-4,  s3.StudentId, fptId, "Viết integration test cho Authentication module.",                               "Mock HttpContext phức tạp.",          "Dùng WebApplicationFactory thay mock."),
                    (-2,  s3.StudentId, fptId, "Cải thiện coverage unit test từ 40% lên 60%.",                                  null,                                  "Tiếp tục đến 80% theo yêu cầu mentor."),
                };

                // ── s4 (Phạm Thị Dung) — HIGH COMPLETION ~85%, Leader Rikkei ──
                // 11 submitted / ~13 required days (joined 18 days ago)
                var s4Logbooks = new[]
                {
                    (-17, s4.StudentId, rikkeiId, "Kickoff Rikkeisoft Internal Portal — phân tích yêu cầu HR module.",          null,                                  "Lên ERD cho module nhân sự."),
                    (-16, s4.StudentId, rikkeiId, "Thiết kế database schema: Employee, Department, LeaveRequest.",              "Cần xác nhận business rules từ PO.",  "Meeting với PO ngày mai."),
                    (-15, s4.StudentId, rikkeiId, "Setup Spring Boot project + Docker Compose.",                                 null,                                  "Config Flyway migration."),
                    (-14, s4.StudentId, rikkeiId, "Implement Employee CRUD API.",                                                null,                                  "Thêm validation và error handling."),
                    (-11, s4.StudentId, rikkeiId, "Implement Leave Request module — submit, approve, reject flow.",             "Logic approval phức tạp với nhiều level.", "Refactor state machine."),
                    (-10, s4.StudentId, rikkeiId, "Viết API test với Postman, tạo collection chia sẻ team.",                    null,                                  "Thêm environment variables."),
                    (-9,  s4.StudentId, rikkeiId, "Tích hợp JWT authentication cho toàn bộ API.",                               null,                                  "Kiểm tra bảo mật endpoint."),
                    (-8,  s4.StudentId, rikkeiId, "Demo sprint 1 cho mentor — nhận feedback về API design.",                    null,                                  "Áp dụng REST best practices theo góp ý."),
                    (-7,  s4.StudentId, rikkeiId, "Implement Timesheet module — clock in/out, overtime calculation.",           "Timezone handling phức tạp.",         "Dùng UTC internally, convert khi display."),
                    (-4,  s4.StudentId, rikkeiId, "Viết unit test JUnit 5 cho service layer — coverage 78%.",                   null,                                  "Đẩy lên 85%."),
                    (0,   s4.StudentId, rikkeiId, "Review code toàn bộ team, chuẩn bị mid-term evaluation.",                   null,                                  "Hoàn thiện tài liệu API Swagger."),
                };

                // ── s5 (Hoàng Văn Em) — POOR COMPLETION, Member Rikkei active group ──
                // 3 logbooks / ~13 required → MissingMany (many absences)
                var s5RikkeiLogbooks = new[]
                {
                    (-15, s5.StudentId, rikkeiId, "Nghiên cứu framework Flutter cơ bản cho mobile app.",                        "Chưa có kinh nghiệm Flutter.",        "Hoàn thành tutorial counter app."),
                    (-8,  s5.StudentId, rikkeiId, "Xây dựng màn hình đăng nhập Flutter — kết nối REST API.",                   "State management chưa chọn được.",    "Thử nghiệm Provider vs Riverpod."),
                    (-3,  s5.StudentId, rikkeiId, "Implement màn hình Employee List với lazy loading.",                          null,                                  "Tiếp tục các màn hình còn lại."),
                };

                // Add s1 logbooks
                foreach (var (daysAgo, studentId, groupId, summary, issue, plan) in s1Logbooks)
                {
                    var dateReport = DateTime.UtcNow.AddDays(daysAgo);
                    _context.Logbooks.Add(Logbook.Create(groupId, studentId, summary, issue, plan, dateReport));
                }

                // Add s2 logbooks
                foreach (var (daysAgo, studentId, groupId, summary, issue, plan) in s2Logbooks)
                {
                    var dateReport = DateTime.UtcNow.AddDays(daysAgo);
                    _context.Logbooks.Add(Logbook.Create(groupId, studentId, summary, issue, plan, dateReport));
                }

                // Add s3 NEW logbooks (skip dates already seeded)
                foreach (var (daysAgo, studentId, groupId, summary, issue, plan) in s3NewLogbooks)
                {
                    var dateReport = DateTime.UtcNow.AddDays(daysAgo).Date;
                    if (!s3ExistingDates.Contains(dateReport))
                        _context.Logbooks.Add(Logbook.Create(groupId, studentId, summary, issue, plan, dateReport));
                }

                // Add s4 logbooks
                foreach (var (daysAgo, studentId, groupId, summary, issue, plan) in s4Logbooks)
                {
                    var dateReport = DateTime.UtcNow.AddDays(daysAgo);
                    _context.Logbooks.Add(Logbook.Create(groupId, studentId, summary, issue, plan, dateReport));
                }

                // Add s5 logbooks in Rikkei active group
                foreach (var (daysAgo, studentId, groupId, summary, issue, plan) in s5RikkeiLogbooks)
                {
                    var dateReport = DateTime.UtcNow.AddDays(daysAgo);
                    _context.Logbooks.Add(Logbook.Create(groupId, studentId, summary, issue, plan, dateReport));
                }

                await _context.SaveChangesAsync();
            }

            // ── 4. Violation Reports (thêm cho s3 và s2 để test) ──
            bool hasExtraViolations = await _context.Set<ViolationReport>()
                .AnyAsync(v => v.StudentId == s3.StudentId && v.OccurredDate < DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)));
            if (!hasExtraViolations)
            {
                // s3: thêm 2 violation nữa (tổng 3) — pattern tái phạm
                _context.Set<ViolationReport>().AddRange(
                    new ViolationReport
                    {
                        ViolationReportId = Guid.NewGuid(),
                        StudentId = s3.StudentId,
                        InternshipGroupId = fptGroup.InternshipId,
                        OccurredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)),
                        Description = "Nộp báo cáo logbook trễ 3 ngày liên tiếp mà không thông báo với mentor."
                    },
                    new ViolationReport
                    {
                        ViolationReportId = Guid.NewGuid(),
                        StudentId = s3.StudentId,
                        InternshipGroupId = fptGroup.InternshipId,
                        OccurredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                        Description = "Không tham gia daily standup trong tuần mà không có lý do chính đáng."
                    }
                );

                // s2: 1 violation nhỏ
                bool s2HasViolation = await _context.Set<ViolationReport>().AnyAsync(v => v.StudentId == s2.StudentId);
                if (!s2HasViolation)
                {
                    _context.Set<ViolationReport>().Add(new ViolationReport
                    {
                        ViolationReportId = Guid.NewGuid(),
                        StudentId = s2.StudentId,
                        InternshipGroupId = fptGroup.InternshipId,
                        OccurredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                        Description = "Nộp báo cáo tuần trễ 1 ngày."
                    });
                }

                await _context.SaveChangesAsync();
            }

            // ── 5. Evaluations: thêm Published evaluations để UniAdmin có thể xem ──
            var midTermCycle = await _context.Set<EvaluationCycle>()
                .Include(c => c.Criteria)
                .FirstOrDefaultAsync(c => c.Name == "Mid-term Spring 2026");

            if (midTermCycle == null) return;

            // Fix MaxScore trên criteria hiện tại (nếu chưa có)
            foreach (var crit in midTermCycle.Criteria)
            {
                if (crit.MaxScore == 0m) crit.MaxScore = 10m;
            }

            // Tạo Final Evaluation cycle nếu chưa có
            var finalCycle = await _context.Set<EvaluationCycle>()
                .Include(c => c.Criteria)
                .FirstOrDefaultAsync(c => c.Name == "Final Evaluation Spring 2026");

            var phaseFptSpring = await _context.InternshipPhases
                .FirstOrDefaultAsync(p => p.Name == "FPT Software Spring 2026");
            if (phaseFptSpring == null) return;

            if (finalCycle == null)
            {
                finalCycle = new EvaluationCycle
                {
                    CycleId   = Guid.NewGuid(),
                    PhaseId   = phaseFptSpring.PhaseId,
                    Name      = "Final Evaluation Spring 2026",
                    StartDate = DateTime.UtcNow.AddDays(5),
                    EndDate   = DateTime.UtcNow.AddDays(25),
                    Status    = EvaluationCycleStatus.Grading
                };
                finalCycle.Criteria.Add(new EvaluationCriteria
                {
                    CriteriaId  = Guid.NewGuid(), CycleId = finalCycle.CycleId,
                    Name        = "Technical Skills",
                    Description = "Code quality, architecture, and problem-solving",
                    MaxScore    = 10m, Weight = 0.60m
                });
                finalCycle.Criteria.Add(new EvaluationCriteria
                {
                    CriteriaId  = Guid.NewGuid(), CycleId = finalCycle.CycleId,
                    Name        = "Soft Skills",
                    Description = "Communication, teamwork, and attitude",
                    MaxScore    = 10m, Weight = 0.40m
                });
                _context.Set<EvaluationCycle>().Add(finalCycle);
                await _context.SaveChangesAsync();
            }

            // Tạo Rikkei Mid-term cycle nếu chưa có
            var phaseRikkeiSpring = await _context.InternshipPhases
                .FirstOrDefaultAsync(p => p.Name == "Rikkeisoft Spring 2026");
            if (phaseRikkeiSpring == null) return;

            var rikkeiMidCycle = await _context.Set<EvaluationCycle>()
                .Include(c => c.Criteria)
                .FirstOrDefaultAsync(c => c.Name == "Mid-term Rikkeisoft Spring 2026");

            if (rikkeiMidCycle == null)
            {
                rikkeiMidCycle = new EvaluationCycle
                {
                    CycleId   = Guid.NewGuid(),
                    PhaseId   = phaseRikkeiSpring.PhaseId,
                    Name      = "Mid-term Rikkeisoft Spring 2026",
                    StartDate = DateTime.UtcNow.AddDays(-8),
                    EndDate   = DateTime.UtcNow.AddDays(12),
                    Status    = EvaluationCycleStatus.Grading
                };
                rikkeiMidCycle.Criteria.Add(new EvaluationCriteria
                {
                    CriteriaId  = Guid.NewGuid(), CycleId = rikkeiMidCycle.CycleId,
                    Name        = "Technical Skills",
                    Description = "Backend development skills and code quality",
                    MaxScore    = 10m, Weight = 0.60m
                });
                rikkeiMidCycle.Criteria.Add(new EvaluationCriteria
                {
                    CriteriaId  = Guid.NewGuid(), CycleId = rikkeiMidCycle.CycleId,
                    Name        = "Professional Skills",
                    Description = "Professionalism, punctuality, and collaboration",
                    MaxScore    = 10m, Weight = 0.40m
                });
                _context.Set<EvaluationCycle>().Add(rikkeiMidCycle);
                await _context.SaveChangesAsync();
            }

            // Helper để thêm Published evaluation nếu chưa tồn tại
            async Task EnsurePublishedEval(
                Guid cycleId, Guid internshipId, Guid studentId,
                Guid evaluatorId, string note,
                List<(Guid CriteriaId, decimal Score, decimal Weight, string Comment)> details)
            {
                var existingEval = await _context.Set<Evaluation>().FirstOrDefaultAsync(
                    e => e.CycleId == cycleId && e.StudentId == studentId);
                
                if (existingEval != null)
                {
                    // If it exists but is not published, we update it
                    if (existingEval.Status != EvaluationStatus.Published)
                    {
                        existingEval.Status = EvaluationStatus.Published;
                        existingEval.Note = note;
                        existingEval.EvaluatorId = evaluatorId;
                        _context.Set<Evaluation>().Update(existingEval);
                    }
                    return;
                }

                var eval = new Evaluation
                {
                    EvaluationId  = Guid.NewGuid(),
                    CycleId       = cycleId,
                    InternshipId  = internshipId,
                    StudentId     = studentId,
                    EvaluatorId   = evaluatorId,
                    Status        = EvaluationStatus.Published,
                    Note          = note
                };
                decimal total = 0;
                foreach (var (criteriaId, score, weight, comment) in details)
                {
                    eval.Details.Add(new EvaluationDetail
                    {
                        DetailId     = Guid.NewGuid(),
                        EvaluationId = eval.EvaluationId,
                        CriteriaId   = criteriaId,
                        Score        = score,
                        Comment      = comment
                    });
                    total += score * weight;
                }
                eval.TotalScore = Math.Round(total, 2);
                _context.Set<Evaluation>().Add(eval);
            }

            var midCriteria   = midTermCycle.Criteria.OrderBy(c => c.Name).ToList(); // Soft Skills, Technical Skills
            var finalCriteria = finalCycle.Criteria.OrderBy(c => c.Name).ToList();
            var rikkeiCriteria = rikkeiMidCycle.Criteria.OrderBy(c => c.Name).ToList();

            // Tìm Technical và Soft criteria cho từng cycle
            var midTech   = midCriteria.FirstOrDefault(c => c.Name.Contains("Technical"));
            var midSoft   = midCriteria.FirstOrDefault(c => c.Name.Contains("Soft") || c.Name.Contains("Soft"));
            var finalTech = finalCriteria.FirstOrDefault(c => c.Name.Contains("Technical"));
            var finalSoft = finalCriteria.FirstOrDefault(c => c.Name.Contains("Soft"));
            var rikkeiTech = rikkeiCriteria.FirstOrDefault(c => c.Name.Contains("Technical"));
            var rikkeiProf = rikkeiCriteria.FirstOrDefault(c => c.Name.Contains("Professional") || c.Name.Contains("Soft"));

            if (midTech == null || midSoft == null || finalTech == null || finalSoft == null
                || rikkeiTech == null || rikkeiProf == null) return;

            // ── s1 Mid-term: Published — Technical=9.0, Soft=8.5 → Total=8.80 ──
            await EnsurePublishedEval(
                midTermCycle.CycleId, fptGroup.InternshipId, s1.StudentId, mentorFpt.UserId,
                "An thể hiện kỹ năng kỹ thuật xuất sắc, chủ động giải quyết vấn đề phức tạp.",
                new List<(Guid, decimal, decimal, string)>
                {
                    (midTech.CriteriaId, 9.0m, midTech.Weight, "Kiến trúc clean, code rõ ràng, unit test đầy đủ."),
                    (midSoft.CriteriaId, 8.5m, midSoft.Weight, "Giao tiếp tốt, hỗ trợ tích cực các thành viên khác.")
                });

            // ── s3 Mid-term: Published — Technical=7.5, Soft=8.0 → Total=7.70 ──
            await EnsurePublishedEval(
                midTermCycle.CycleId, fptGroup.InternshipId, s3.StudentId, mentorFpt.UserId,
                "Cường có tiến bộ nhưng cần chủ động hơn trong việc báo cáo vấn đề kịp thời.",
                new List<(Guid, decimal, decimal, string)>
                {
                    (midTech.CriteriaId, 7.5m, midTech.Weight, "Code chức năng nhưng thiếu test coverage và error handling."),
                    (midSoft.CriteriaId, 8.0m, midSoft.Weight, "Thái độ tốt, chịu học hỏi, cần cải thiện chủ động báo cáo.")
                });

            // ── s1 Final: Published — Technical=9.5, Soft=9.0 → Total=9.30 ──
            await EnsurePublishedEval(
                finalCycle.CycleId, fptGroup.InternshipId, s1.StudentId, mentorFpt.UserId,
                "Kết quả xuất sắc. An hoàn thành tất cả tasks đúng hạn với chất lượng cao.",
                new List<(Guid, decimal, decimal, string)>
                {
                    (finalTech.CriteriaId, 9.5m, finalTech.Weight, "Implement feature phức tạp, tối ưu performance, viết test toàn diện."),
                    (finalSoft.CriteriaId, 9.0m, finalSoft.Weight, "Leadership tốt, hướng dẫn nhiệt tình cho đồng nghiệp.")
                });

            // ── s3 Final: Published — Technical=8.0, Soft=7.0 → Total=7.60 ──
            await EnsurePublishedEval(
                finalCycle.CycleId, fptGroup.InternshipId, s3.StudentId, mentorFpt.UserId,
                "Cường cải thiện đáng kể so với mid-term nhưng vẫn còn một số điểm cần phát huy.",
                new List<(Guid, decimal, decimal, string)>
                {
                    (finalTech.CriteriaId, 8.0m, finalTech.Weight, "Cải thiện rõ rệt về code quality và testing."),
                    (finalSoft.CriteriaId, 7.0m, finalSoft.Weight, "Vẫn còn trường hợp vắng standup, cần kỷ luật hơn.")
                });

            // ── s4 Rikkei Mid-term: Published — Technical=8.5, Professional=8.0 → Total=8.30 ──
            await EnsurePublishedEval(
                rikkeiMidCycle.CycleId, rikkeiGroup.InternshipId, s4.StudentId, mentorRikkei.UserId,
                "Dung thể hiện tốt vai trò team leader, quản lý tốt tiến độ nhóm.",
                new List<(Guid, decimal, decimal, string)>
                {
                    (rikkeiTech.CriteriaId, 8.5m, rikkeiTech.Weight, "API design tốt, biết áp dụng design patterns phù hợp."),
                    (rikkeiProf.CriteriaId, 8.0m, rikkeiProf.Weight, "Đúng giờ, chủ động, phối hợp nhóm hiệu quả.")
                });

            // s5 Rikkei Mid-term: Draft only — KHÔNG Published, UniAdmin không thấy
            bool s5DraftExists = await _context.Set<Evaluation>().AnyAsync(
                e => e.CycleId == rikkeiMidCycle.CycleId && e.StudentId == s5.StudentId);
            if (!s5DraftExists)
            {
                var s5Draft = new Evaluation
                {
                    EvaluationId = Guid.NewGuid(),
                    CycleId      = rikkeiMidCycle.CycleId,
                    InternshipId = rikkeiGroup.InternshipId,
                    StudentId    = s5.StudentId,
                    EvaluatorId  = mentorRikkei.UserId,
                    Status       = EvaluationStatus.Draft,
                    Note         = "Em cần cải thiện nhiều hơn về chuyên cần.",
                    TotalScore   = null
                };
                _context.Set<Evaluation>().Add(s5Draft);
            }

            await _context.SaveChangesAsync();

            // ── 6. WorkItems cho s1/s2 (FPT) và s4/s5 (Rikkei) + link to logbooks ──
            var proj3Detail = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectName == "IOC v2.0 Platform");
            var rikkeiPortalProj = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectName == "Rikkeisoft Internal Portal");

            if (proj3Detail != null)
            {
                // 6a. WorkItems cho s1 (FPT Leader)
                bool s1WiExists = await _context.WorkItems
                    .AnyAsync(w => w.ProjectId == proj3Detail.ProjectId && w.AssigneeId == s1.StudentId);
                if (!s1WiExists)
                {
                    _context.WorkItems.AddRange(
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "Setup CI/CD pipeline GitHub Actions", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 3, AssigneeId = s1.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-24)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "Implement Identity module (JWT + refresh token)", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 8, AssigneeId = s1.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-18)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "CRUD API Student module + Swagger docs", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 5, AssigneeId = s1.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-15)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "Fix bug pagination off-by-one", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.Medium, StoryPoint = 2, AssigneeId = s1.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "Notification module — SignalR real-time", Type = WorkItemType.Task, Status = WorkItemStatus.InProgress, Priority = Priority.Medium, StoryPoint = 5, AssigneeId = s1.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)) }
                    );
                }

                // 6b. WorkItems cho s2 (FPT Frontend)
                bool s2WiExists = await _context.WorkItems
                    .AnyAsync(w => w.ProjectId == proj3Detail.ProjectId && w.AssigneeId == s2.StudentId);
                if (!s2WiExists)
                {
                    _context.WorkItems.AddRange(
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "Thiết kế wireframe & Figma dashboard", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 3, AssigneeId = s2.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-25)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "React component StudentListPage", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 5, AssigneeId = s2.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-22)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "Fix responsive layout mobile breakpoint 375px", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.Medium, StoryPoint = 2, AssigneeId = s2.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = proj3Detail.ProjectId, Title = "WeeklyLogbook calendar component", Type = WorkItemType.Task, Status = WorkItemStatus.InProgress, Priority = Priority.High, StoryPoint = 8, AssigneeId = s2.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)) }
                    );
                }

                await _context.SaveChangesAsync();

                // 6c. Link WorkItems → FPT logbooks
                bool fptLogbooksLinked = await _context.Logbooks
                    .AnyAsync(l => l.InternshipId == fptGroup.InternshipId
                                && l.StudentId == s1.StudentId
                                && l.WorkItems.Any());
                if (!fptLogbooksLinked)
                {
                    var s1Wi = await _context.WorkItems
                        .Where(w => w.ProjectId == proj3Detail.ProjectId && w.AssigneeId == s1.StudentId).ToListAsync();
                    var s2Wi = await _context.WorkItems
                        .Where(w => w.ProjectId == proj3Detail.ProjectId && w.AssigneeId == s2.StudentId).ToListAsync();
                    var s3Wi = await _context.WorkItems
                        .Where(w => w.ProjectId == proj3Detail.ProjectId && w.AssigneeId == s3.StudentId).ToListAsync();

                    var fptLogbooks = await _context.Logbooks
                        .Include(l => l.WorkItems)
                        .Where(l => l.InternshipId == fptGroup.InternshipId)
                        .ToListAsync();

                    // Mỗi logbook của student liên kết với 1-2 WorkItem của họ (phản ánh thực tế)
                    foreach (var lb in fptLogbooks.Where(l => l.StudentId == s1.StudentId))
                        foreach (var wi in s1Wi.Take(2))
                            if (!lb.WorkItems.Any(x => x.WorkItemId == wi.WorkItemId))
                                lb.WorkItems.Add(wi);

                    foreach (var lb in fptLogbooks.Where(l => l.StudentId == s2.StudentId))
                        foreach (var wi in s2Wi.Take(2))
                            if (!lb.WorkItems.Any(x => x.WorkItemId == wi.WorkItemId))
                                lb.WorkItems.Add(wi);

                    foreach (var lb in fptLogbooks.Where(l => l.StudentId == s3.StudentId))
                        foreach (var wi in s3Wi.Take(2))
                            if (!lb.WorkItems.Any(x => x.WorkItemId == wi.WorkItemId))
                                lb.WorkItems.Add(wi);

                    await _context.SaveChangesAsync();
                }
            }

            if (rikkeiPortalProj != null)
            {
                // 6d. WorkItems cho s4 (Rikkei Leader — Java backend)
                bool s4WiExists = await _context.WorkItems
                    .AnyAsync(w => w.ProjectId == rikkeiPortalProj.ProjectId && w.AssigneeId == s4.StudentId);
                if (!s4WiExists)
                {
                    _context.WorkItems.AddRange(
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = rikkeiPortalProj.ProjectId, Title = "ERD & database schema cho HR module", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 3, AssigneeId = s4.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-16)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = rikkeiPortalProj.ProjectId, Title = "Employee CRUD API (Spring Boot)", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 5, AssigneeId = s4.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-13)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = rikkeiPortalProj.ProjectId, Title = "Leave Request module (submit/approve/reject)", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 8, AssigneeId = s4.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-9)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = rikkeiPortalProj.ProjectId, Title = "JWT authentication toàn bộ REST API", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 5, AssigneeId = s4.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = rikkeiPortalProj.ProjectId, Title = "Timesheet module — clock in/out & overtime", Type = WorkItemType.Task, Status = WorkItemStatus.InProgress, Priority = Priority.Medium, StoryPoint = 8, AssigneeId = s4.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)) }
                    );
                }

                // 6e. WorkItems cho s5 (Rikkei — Flutter mobile)
                bool s5WiExists = await _context.WorkItems
                    .AnyAsync(w => w.ProjectId == rikkeiPortalProj.ProjectId && w.AssigneeId == s5.StudentId);
                if (!s5WiExists)
                {
                    _context.WorkItems.AddRange(
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = rikkeiPortalProj.ProjectId, Title = "Flutter tutorial & setup môi trường", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.Low, StoryPoint = 2, AssigneeId = s5.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = rikkeiPortalProj.ProjectId, Title = "Màn hình đăng nhập Flutter + REST API", Type = WorkItemType.Task, Status = WorkItemStatus.Done, Priority = Priority.High, StoryPoint = 5, AssigneeId = s5.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)) },
                        new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = rikkeiPortalProj.ProjectId, Title = "Employee List screen với lazy loading", Type = WorkItemType.Task, Status = WorkItemStatus.InProgress, Priority = Priority.High, StoryPoint = 5, AssigneeId = s5.StudentId, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)) }
                    );
                }

                await _context.SaveChangesAsync();

                // 6f. Link WorkItems → Rikkei logbooks
                bool rikkeiLogbooksLinked = await _context.Logbooks
                    .AnyAsync(l => l.InternshipId == rikkeiGroup.InternshipId
                                && l.StudentId == s4.StudentId
                                && l.WorkItems.Any());
                if (!rikkeiLogbooksLinked)
                {
                    var s4Wi = await _context.WorkItems
                        .Where(w => w.ProjectId == rikkeiPortalProj.ProjectId && w.AssigneeId == s4.StudentId).ToListAsync();
                    var s5Wi = await _context.WorkItems
                        .Where(w => w.ProjectId == rikkeiPortalProj.ProjectId && w.AssigneeId == s5.StudentId).ToListAsync();

                    var rikkeiLogbooks = await _context.Logbooks
                        .Include(l => l.WorkItems)
                        .Where(l => l.InternshipId == rikkeiGroup.InternshipId)
                        .ToListAsync();

                    foreach (var lb in rikkeiLogbooks.Where(l => l.StudentId == s4.StudentId))
                        foreach (var wi in s4Wi.Take(2))
                            if (!lb.WorkItems.Any(x => x.WorkItemId == wi.WorkItemId))
                                lb.WorkItems.Add(wi);

                    foreach (var lb in rikkeiLogbooks.Where(l => l.StudentId == s5.StudentId))
                        foreach (var wi in s5Wi.Take(2))
                            if (!lb.WorkItems.Any(x => x.WorkItemId == wi.WorkItemId))
                                lb.WorkItems.Add(wi);

                    await _context.SaveChangesAsync();
                }
            }

            // ── 7. Violations cho Rikkei (s5 vắng nhiều, poor performer) ──
            bool rikkeiS5ViolationExists = await _context.Set<ViolationReport>()
                .AnyAsync(v => v.StudentId == s5.StudentId && v.InternshipGroupId == rikkeiGroup.InternshipId);
            if (!rikkeiS5ViolationExists)
            {
                _context.Set<ViolationReport>().AddRange(
                    new ViolationReport
                    {
                        ViolationReportId = Guid.NewGuid(),
                        StudentId         = s5.StudentId,
                        InternshipGroupId = rikkeiGroup.InternshipId,
                        OccurredDate      = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12)),
                        Description       = "Vắng buổi weekly meeting không thông báo trước.",
                        CreatedBy         = mentorRikkei.UserId,
                        CreatedAt         = DateTime.UtcNow.AddDays(-11)
                    },
                    new ViolationReport
                    {
                        ViolationReportId = Guid.NewGuid(),
                        StudentId         = s5.StudentId,
                        InternshipGroupId = rikkeiGroup.InternshipId,
                        OccurredDate      = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                        Description       = "Nộp task trễ deadline 2 ngày liên tiếp mà không có báo cáo.",
                        CreatedBy         = mentorRikkei.UserId,
                        CreatedAt         = DateTime.UtcNow.AddDays(-4)
                    }
                );
                await _context.SaveChangesAsync();
            }

            // ── 8. Gán CreatedBy cho các violations chưa có reporter ──
            var unattributedFptViolations = await _context.Set<ViolationReport>()
                .Where(v => v.CreatedBy == null
                         && (v.StudentId == s2.StudentId || v.StudentId == s3.StudentId))
                .ToListAsync();
            foreach (var v in unattributedFptViolations)
            {
                v.CreatedBy = mentorFpt.UserId;
                v.UpdatedAt = v.CreatedAt.AddMinutes(5);
            }

            // ── 9. Set UpdatedAt trên Published evaluations (simulate thời gian publish) ──
            var evalsMissingPublishTime = await _context.Set<Evaluation>()
                .Where(e => e.Status == EvaluationStatus.Published && e.UpdatedAt == null)
                .ToListAsync();
            foreach (var eval in evalsMissingPublishTime)
                eval.UpdatedAt = eval.CreatedAt.AddHours(2);

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


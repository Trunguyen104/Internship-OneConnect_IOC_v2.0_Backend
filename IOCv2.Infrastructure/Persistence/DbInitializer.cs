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

        private async Task SeedBulkUnassignTestStudent()
        {
            // Deterministic data for testing BulkUnassignHandler
            const string testEmail = "bulkunassign.test@fptu.edu.vn";
            if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == testEmail))
                return;

            var fptUniversity = await _context.Universities.FirstOrDefaultAsync(u => u.Code == "FPTU");
            if (fptUniversity == null) return;

            // Pick an existing job to attach the UniAssign application to
            var job = await _context.Jobs.FirstOrDefaultAsync(j => j.Title == "FPT QA Automation Intern");
            var springTerm = await _context.Terms.FirstOrDefaultAsync(t => t.Name == "Spring 2026" && t.UniversityId == fptUniversity.UniversityId);

            // Create user + student
            var passHash = _passwordService.HashPassword("Test@1234");
            var userId = Guid.NewGuid();
            var userCode = await _userService.GenerateUserCodeAsync(Domain.Enums.UserRole.Student, CancellationToken.None);
            var user = new Domain.Entities.User(userId, userCode, testEmail, "BulkUnassign Test Student", Domain.Enums.UserRole.Student, passHash);
            user.UpdateProfile(user.FullName, "0987654321", null, Domain.Enums.UserGender.Male, new DateOnly(2003, 1, 1), "Hà Nội");
            user.SetStatus(Domain.Enums.UserStatus.Active);
            _context.Users.Add(user);

            var studentId = Guid.NewGuid();
            var student = new Domain.Entities.Student
            {
                StudentId = studentId,
                UserId = user.UserId,
                InternshipStatus = Domain.Enums.StudentStatus.APPLIED,
                Major = "Software Engineering",
                ClassName = "SE-TEST"
            };
            _context.Students.Add(student);

            var universityUser = new Domain.Entities.UniversityUser
            {
                UniversityUserId = Guid.NewGuid(),
                UserId = user.UserId,
                UniversityId = fptUniversity.UniversityId
            };
            universityUser.UpdateMetadata("Test Student", null, null);
            _context.UniversityUsers.Add(universityUser);

            // Create a UniAssign application in PendingAssignment (so BulkUnassign will find it)
            if (job != null && springTerm != null)
            {
                var app = new Domain.Entities.InternshipApplication
                {
                    ApplicationId = Guid.NewGuid(),
                    EnterpriseId = job.EnterpriseId,
                    TermId = springTerm.TermId,
                    StudentId = student.StudentId,
                    JobId = job.JobId,
                    Status = Domain.Enums.InternshipApplicationStatus.PendingAssignment,
                    Source = Domain.Enums.ApplicationSource.UniAssign,
                    AppliedAt = DateTime.UtcNow,
                    UniversityId = fptUniversity.UniversityId,
                    JobPostingTitle = job.Title
                };
                _context.InternshipApplications.Add(app);
            }

            await _context.SaveChangesAsync();
        }

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
            public static readonly Guid MentorFptBackupId = new Guid("55555555-5555-5555-5555-555555550011");
            public static readonly Guid SchoolAdminFptCtId = new Guid("33333333-3333-3333-3333-333333330002");
            public static readonly Guid EntAdminRikkeisoftId = new Guid("44444444-4444-4444-4444-444444440002");
            public static readonly Guid MentorRikkeisoftId = new Guid("55555555-5555-5555-5555-555555550002");
            public static readonly Guid MentorRikkeisoftBackupId = new Guid("55555555-5555-5555-5555-555555550012");

            // Added HR seed ids for deterministic seeding
            public static readonly Guid HrFptId = new Guid("77777777-7777-7777-7777-777777770001");
            public static readonly Guid HrRikkeisoftId = new Guid("77777777-7777-7777-7777-777777770002");

            // Deterministic EnterpriseUser IDs for mentors — phải cố định để group.MentorId khớp sau mỗi lần seed
            // mentor@fptsoftware.com  → dùng EnterpriseUserId này khi tạo project cho FPT groups
            // mentor@rikkeisoft.com   → dùng EnterpriseUserId này khi tạo project cho Rikkeisoft groups
            public static readonly Guid MentorFptEuId = new Guid("88888888-8888-8888-8888-888888880001");
            public static readonly Guid MentorRikkeisoftEuId = new Guid("88888888-8888-8888-8888-888888880002");
            public static readonly Guid MentorFptBackupEuId = new Guid("88888888-8888-8888-8888-888888880011");
            public static readonly Guid MentorRikkeisoftBackupEuId = new Guid("88888888-8888-8888-8888-888888880012");

            // Swagger unhappy-path fixtures for inline assign/reassign mentor
            public static readonly Guid SwaggerWrongEnterpriseMentorUserId = new Guid("95555555-5555-5555-5555-555555550001");
            public static readonly Guid SwaggerWrongEnterpriseMentorEuId = new Guid("98888888-8888-8888-8888-888888880001");
            public static readonly Guid SwaggerNotMentorUserId = new Guid("96666666-6666-6666-6666-666666660001");
            public static readonly Guid SwaggerNotMentorEuId = new Guid("98888888-8888-8888-8888-888888880002");

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

            // Deterministic ids for student11 (UniAssign) and student12 (SelfApply) placement tests
            public static readonly Guid Student11UserId = new Guid("66666666-6666-6666-6666-666666660011");
            public static readonly Guid Student12UserId = new Guid("66666666-6666-6666-6666-666666660012");
            public static readonly Guid Student13UserId = new Guid("66666666-6666-6666-6666-666666660013");
            public static readonly Guid Student14UserId = new Guid("66666666-6666-6666-6666-666666660014");

            // ── Deterministic IDs cho demo flow: GROUP & ASSIGNMENT (HR) ──────────────
            // Flow 5 bước: Tạo Group → Update → Add Student → Remove Student → Assign Mentor
            // Phase target: "FPT Software Summer 2026" (Open) — không đụng data Spring 2026 hiện có
            //
            // Phân vai:
            //   A + B  → thành viên ban đầu khi Tạo Group (bước 1)
            //   A      → bị Remove ở bước 4 (xóa khỏi group)
            //   C      → free, dùng cho Add Student (bước 3)
            //   D–O    → pool dự phòng, xuất hiện trong dropdown để UI trông đầy đủ
            //
            // Pre-seeded group: "FPT Demo Presentation Group"
            //   → Active, không có Mentor, KHÔNG có thành viên từ pool demo
            //   → Tất cả 15 sv đều FREE (không bị StudentAlreadyInActiveGroup)
            //   → Group tồn tại để UI có dữ liệu nền, không dùng trong demo flow chính
            public static readonly Guid DemoGroupStudentAUserId = new Guid("DDDD0000-0000-0000-0000-000000000001");
            public static readonly Guid DemoGroupStudentBUserId = new Guid("DDDD0000-0000-0000-0000-000000000002");
            public static readonly Guid DemoGroupStudentCUserId = new Guid("DDDD0000-0000-0000-0000-000000000003");
            public static readonly Guid DemoGroupStudentDUserId = new Guid("DDDD0000-0000-0000-0000-000000000004");
            public static readonly Guid DemoGroupStudentEUserId = new Guid("DDDD0000-0000-0000-0000-000000000005");
            public static readonly Guid DemoGroupStudentFUserId = new Guid("DDDD0000-0000-0000-0000-000000000006");
            public static readonly Guid DemoGroupStudentGUserId = new Guid("DDDD0000-0000-0000-0000-000000000007");
            public static readonly Guid DemoGroupStudentHUserId = new Guid("DDDD0000-0000-0000-0000-000000000008");
            public static readonly Guid DemoGroupStudentIUserId = new Guid("DDDD0000-0000-0000-0000-000000000009");
            public static readonly Guid DemoGroupStudentJUserId = new Guid("DDDD0000-0000-0000-0000-00000000000A");
            public static readonly Guid DemoGroupStudentKUserId = new Guid("DDDD0000-0000-0000-0000-00000000000B");
            public static readonly Guid DemoGroupStudentLUserId = new Guid("DDDD0000-0000-0000-0000-00000000000C");
            public static readonly Guid DemoGroupStudentMUserId = new Guid("DDDD0000-0000-0000-0000-00000000000D");
            public static readonly Guid DemoGroupStudentNUserId = new Guid("DDDD0000-0000-0000-0000-00000000000E");
            public static readonly Guid DemoGroupStudentOUserId = new Guid("DDDD0000-0000-0000-0000-00000000000F");
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
            
            await SeedBulkUnassignTestStudent();

            await SeedInternshipGroups();
            await SeedDemoGroupAssignmentStudents();
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
            await SeedStudentTermEnterpriseFromApplications();

            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }

            await EnsureActiveProjectGroupConstraintAsync();
        }

        private async Task EnsureActiveProjectGroupConstraintAsync()
        {
            if (!_context.Database.IsRelational())
            {
                return;
            }

            await _context.Database.ExecuteSqlRawAsync(@"
                WITH ranked AS (
                    SELECT project_id,
                           ROW_NUMBER() OVER (
                               PARTITION BY internship_id
                               ORDER BY COALESCE(updated_at, created_at) DESC, created_at DESC, project_id
                           ) AS rn
                    FROM projects
                    WHERE deleted_at IS NULL
                      AND internship_id IS NOT NULL
                      AND operational_status = 1
                )
                UPDATE projects p
                SET operational_status = 0,
                    updated_at = CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
                FROM ranked r
                WHERE p.project_id = r.project_id
                  AND r.rn > 1;
            ");

            await _context.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX IF NOT EXISTS uix_projects_internship_id_active
                ON projects (internship_id)
                WHERE deleted_at IS NULL AND internship_id IS NOT NULL AND operational_status = 1;
            ");
        }

        private async Task SeedUniversities()
        {
            if (!await _context.Universities.AnyAsync())
            {
                var universities = new List<University>
                {
                    University.Create("FPTU", "FPT University", "Hoa Lac Hi-Tech Park, Hanoi", null, "fptu@fpt.edu.vn"),
                    University.Create("FPTU-CT", "FPT University Can Tho", "600 Nguyen Van Cu, Ninh Kieu, Can Tho", null, "fptuct@fpt.edu.vn")
                };
                await _context.Universities.AddRangeAsync(universities);
                await _context.SaveChangesAsync();
            }
        }
        // New helper: fill missing StudentTerm.EnterpriseId using each student's InternshipApplication.EnterpriseId (if present)
        private async Task SeedStudentTermEnterpriseFromApplications()
        {
            // Find student-terms that are missing EnterpriseId
            var termsMissingEnterprise = await _context.StudentTerms
                .Include(st => st.Student).Include(st => st.Term)
                .Where(st => st.EnterpriseId == null && st.PlacementStatus == PlacementStatus.Placed)
                .ToListAsync();

            if (!termsMissingEnterprise.Any())
                return;
            foreach (var st in termsMissingEnterprise)
            {
                var enterpriseId = await _context.InternshipApplications
                    .Where(app => app.StudentId == st.StudentId && app.TermId == st.TermId)
                    .Select(app => app.EnterpriseId)
                    .FirstOrDefaultAsync();
                st.EnterpriseId = enterpriseId;
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
                        ContactEmail = "contact@fpt-software.com",
                        Status = EnterpriseStatus.Active
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
                        ContactEmail = "contact@rikkeisoft.com",
                        Status = EnterpriseStatus.Active
                    }
                };
                await _context.Enterprises.AddRangeAsync(enterprises);
                await _context.SaveChangesAsync();
            }
        }

        // New: seed a couple of test jobs tied to seeded enterprises (use EF entities)
        private async Task SeedJobs()
        {
            var fptSpring = await _context.InternshipPhases
                .FirstOrDefaultAsync(p => p.Name == "FPT Software Spring 2026");
            var fptSummer = await _context.InternshipPhases
                .FirstOrDefaultAsync(p => p.Name == "FPT Software Summer 2026");
            var rikkeiSpring = await _context.InternshipPhases
                .FirstOrDefaultAsync(p => p.Name == "Rikkeisoft Spring 2026");

            if (fptSpring == null || fptSummer == null || rikkeiSpring == null)
                return;

            async Task EnsureJob(
                Guid enterpriseId,
                Guid internshipPhaseId,
                string title,
                string position,
                JobStatus status,
                string description,
                string requirements,
                string benefit,
                string location,
                DateTime? expireDate,
                DateTime? startDate,
                DateTime? endDate)
            {
                var job = await _context.Jobs.FirstOrDefaultAsync(j => j.EnterpriseId == enterpriseId && j.Title == title);
                if (job == null)
                {
                    job = Job.Create(enterpriseId, internshipPhaseId, title, description, requirements, benefit, location, expireDate);
                    _context.Jobs.Add(job);
                }

                job.InternshipPhaseId = internshipPhaseId;
                job.Title = title;
                job.Position = position;
                job.Status = status;
                job.Description = description;
                job.Requirements = requirements;
                job.Benefit = benefit;
                job.Location = location;
                job.ExpireDate = expireDate;
                job.StartDate = startDate;
                job.EndDate = endDate;
            }

            await EnsureJob(
                SeedIds.FptSoftwareId,
                fptSpring.PhaseId,
                "FPT Backend Platform Intern",
                "Backend Intern",
                JobStatus.PUBLISHED,
                "Build and maintain internship platform APIs, workflows, and reporting.",
                "C#, ASP.NET Core, EF Core, PostgreSQL, REST",
                "Mentorship, stipend, certificate, real product ownership",
                "Ha Noi (Hybrid)",
                DateTime.UtcNow.AddDays(25),
                DateTime.UtcNow.AddDays(-20),
                DateTime.UtcNow.AddDays(70));

            await EnsureJob(
                SeedIds.FptSoftwareId,
                fptSpring.PhaseId,
                "FPT QA Automation Intern",
                "QA Intern",
                JobStatus.PUBLISHED,
                "Build API/UI automation suites and improve regression quality.",
                "Postman, Playwright, C#, test design",
                "Mentorship and cross-team testing exposure",
                "Ha Noi (On-site)",
                DateTime.UtcNow.AddDays(18),
                DateTime.UtcNow.AddDays(-18),
                DateTime.UtcNow.AddDays(72));

            await EnsureJob(
                SeedIds.FptSoftwareId,
                fptSummer.PhaseId,
                "FPT Data Engineering Intern",
                "Data Intern",
                JobStatus.DRAFT,
                "Prepare ETL and analytics pipelines for internship KPI dashboards.",
                "SQL, Python, data modeling",
                "Stipend and technical coaching",
                "Ha Noi",
                DateTime.UtcNow.AddDays(55),
                DateTime.UtcNow.AddDays(30),
                DateTime.UtcNow.AddDays(120));

            await EnsureJob(
                SeedIds.RikkeisoftId,
                rikkeiSpring.PhaseId,
                "Rikkeisoft Java Backend Intern",
                "Backend Intern",
                JobStatus.PUBLISHED,
                "Develop service modules for internal platforms and APIs.",
                "Java/Spring Boot, SQL, Git",
                "Mentorship, weekly review and stipend",
                "Ha Noi (On-site)",
                DateTime.UtcNow.AddDays(20),
                DateTime.UtcNow.AddDays(-15),
                DateTime.UtcNow.AddDays(65));

            await EnsureJob(
                SeedIds.RikkeisoftId,
                rikkeiSpring.PhaseId,
                "Rikkeisoft Mobile Flutter Intern",
                "Mobile Intern",
                JobStatus.PUBLISHED,
                "Develop and maintain Flutter modules for employee apps.",
                "Flutter, Dart, REST API",
                "Stipend and real mobile project experience",
                "Ha Noi",
                DateTime.UtcNow.AddDays(15),
                DateTime.UtcNow.AddDays(-12),
                DateTime.UtcNow.AddDays(66));

            await EnsureJob(
                SeedIds.RikkeisoftId,
                rikkeiSpring.PhaseId,
                "Rikkeisoft Product Analyst Intern",
                "Business Analyst Intern",
                JobStatus.CLOSED,
                "Support requirement gathering and product process documentation.",
                "Communication, documentation, analytical thinking",
                "Mentorship and BA process training",
                "Ha Noi",
                DateTime.UtcNow.AddDays(-5),
                DateTime.UtcNow.AddDays(-30),
                DateTime.UtcNow.AddDays(30));

            await _context.SaveChangesAsync();
        }

        private async Task SeedUsers()
        {
            var passHash = _passwordService.HashPassword("Admin@123");
            var universityList = await _context.Universities.ToListAsync();
            var enterpriseList = await _context.Enterprises.ToListAsync();
            var existingUsers = await _context.Users
                .IgnoreQueryFilters()
                .Select(u => new { u.Email, u.PhoneNumber })
                .ToListAsync();
            var existingEmails = existingUsers.Select(u => u.Email).ToHashSet();
            var existingPhones = existingUsers.Where(u => u.PhoneNumber != null).Select(u => u.PhoneNumber!).ToHashSet();

            CancellationToken cancellationToken = default;
            int phoneCounter = 1000;

            string GetUniquePhone()
            {
                while (existingPhones.Contains($"098765{phoneCounter}"))
                {
                    phoneCounter++;
                }
                var phone = $"098765{phoneCounter++}";
                existingPhones.Add(phone);
                return phone;
            }

            // 1. Super Admin
            if (!await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.SuperAdmin))
            {
                var userId = SeedIds.SuperAdminId;
                var userCode = await _userService.GenerateUserCodeAsync(UserRole.SuperAdmin, cancellationToken);
                var superAdmin = new User(userId, userCode, "admin@iocv2.com", "Super Administrator", UserRole.SuperAdmin, passHash);
                superAdmin.UpdateProfile(superAdmin.FullName, GetUniquePhone(), null, UserGender.Male, new DateOnly(1980, 1, 1), "Hà Nội");
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
                    user.UpdateProfile(user.FullName, GetUniquePhone(), null, UserGender.Male, new DateOnly(1985, 1, 1), ent.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(adminEmail);
                    var entAdmin = new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), UserId = user.UserId, EnterpriseId = ent.EnterpriseId };
                    entAdmin.UpdateMetadata("Enterprise Administrator", null, null);
                    _context.EnterpriseUsers.Add(entAdmin);

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
                    user.UpdateProfile(user.FullName, GetUniquePhone(), null, UserGender.Male, new DateOnly(1990, 1, 1), ent.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(mentorEmail);
                    var mentorEu = new EnterpriseUser { EnterpriseUserId = mentorEuId, UserId = user.UserId, EnterpriseId = ent.EnterpriseId };
                    mentorEu.UpdateMetadata("Technical Mentor", null, null);
                    _context.EnterpriseUsers.Add(mentorEu);

                }

                // Additional mentors make inline assign/reassign happy/unhappy paths reproducible.
                var backupMentorEmail = $"mentor.backup@{baseName}.com";
                if (!existingEmails.Contains(backupMentorEmail))
                {
                    var backupMentorId = ent.EnterpriseId == SeedIds.FptSoftwareId
                        ? SeedIds.MentorFptBackupId
                        : ent.EnterpriseId == SeedIds.RikkeisoftId
                            ? SeedIds.MentorRikkeisoftBackupId
                            : Guid.NewGuid();

                    var backupMentorEuId = ent.EnterpriseId == SeedIds.FptSoftwareId
                        ? SeedIds.MentorFptBackupEuId
                        : ent.EnterpriseId == SeedIds.RikkeisoftId
                            ? SeedIds.MentorRikkeisoftBackupEuId
                            : Guid.NewGuid();

                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Mentor, cancellationToken);
                    var user = new User(backupMentorId, userCode, backupMentorEmail, $"Backup Mentor {ent.Name}", UserRole.Mentor, passHash);
                    user.UpdateProfile(user.FullName, GetUniquePhone(), null, UserGender.Female, new DateOnly(1991, 6, 1), ent.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(backupMentorEmail);
                    var backupMentorEu = new EnterpriseUser
                    {
                        EnterpriseUserId = backupMentorEuId,
                        UserId = user.UserId,
                        EnterpriseId = ent.EnterpriseId
                    };
                    backupMentorEu.UpdateMetadata("Senior Technical Mentor", null, null);
                    _context.EnterpriseUsers.Add(backupMentorEu);
                }

                // HR account
                var hrEmail = $"hr@{baseName}.com";
                if (!existingEmails.Contains(hrEmail))
                {
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.HR, cancellationToken);
                    var user = new User(hrId, userCode, hrEmail, $"HR {ent.Name}", UserRole.HR, passHash);
                    user.UpdateProfile(user.FullName, GetUniquePhone(), null, UserGender.Female, new DateOnly(1992, 1, 1), ent.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(hrEmail);
                    var hrEu = new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), UserId = user.UserId, EnterpriseId = ent.EnterpriseId };
                    hrEu.UpdateMetadata("HR", null, null);
                    _context.EnterpriseUsers.Add(hrEu);

                }
            }

            // Swagger labels for direct unhappy testing of AssignMentor endpoint:
            // 1) wrong-enterprise mentor: role Mentor nhưng thuộc enterprise khác
            // 2) not-mentor user: cùng enterprise nhưng role != Mentor
            var fptEnterprise = enterpriseList.FirstOrDefault(e => e.EnterpriseId == SeedIds.FptSoftwareId);
            var rikkeiEnterprise = enterpriseList.FirstOrDefault(e => e.EnterpriseId == SeedIds.RikkeisoftId);

            if (fptEnterprise != null && rikkeiEnterprise != null)
            {
                var wrongEnterpriseMentorEmail = "swagger.unhappy.wrong-enterprise.mentor@rikkeisoft.com";
                var wrongEnterpriseMentorUser = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == wrongEnterpriseMentorEmail, cancellationToken);

                if (wrongEnterpriseMentorUser == null)
                {
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Mentor, cancellationToken);
                    wrongEnterpriseMentorUser = new User(
                        SeedIds.SwaggerWrongEnterpriseMentorUserId,
                        userCode,
                        wrongEnterpriseMentorEmail,
                        "[SWAGGER-UNHAPPY] Wrong Enterprise Mentor",
                        UserRole.Mentor,
                        passHash);
                    wrongEnterpriseMentorUser.UpdateProfile(
                        wrongEnterpriseMentorUser.FullName,
                        GetUniquePhone(),
                        null,
                        UserGender.Female,
                        new DateOnly(1992, 2, 2),
                        rikkeiEnterprise.Address);
                    wrongEnterpriseMentorUser.SetStatus(UserStatus.Active);
                    _context.Users.Add(wrongEnterpriseMentorUser);
                    existingEmails.Add(wrongEnterpriseMentorEmail);
                }

                bool hasWrongEnterpriseEu = await _context.EnterpriseUsers
                    .AnyAsync(eu => eu.UserId == wrongEnterpriseMentorUser.UserId && eu.EnterpriseId == rikkeiEnterprise.EnterpriseId, cancellationToken);
                if (!hasWrongEnterpriseEu)
                {
                    var wrongEntEu = new EnterpriseUser
                    {
                        EnterpriseUserId = SeedIds.SwaggerWrongEnterpriseMentorEuId,
                        UserId = wrongEnterpriseMentorUser.UserId,
                        EnterpriseId = rikkeiEnterprise.EnterpriseId
                    };
                    wrongEntEu.UpdateMetadata("[SWAGGER-UNHAPPY] Mentor belongs to other enterprise", null, null);
                    _context.EnterpriseUsers.Add(wrongEntEu);
                }

                var notMentorEmail = "swagger.unhappy.not-mentor@fptsoftware.com";
                var notMentorUser = await _context.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Email == notMentorEmail, cancellationToken);

                if (notMentorUser == null)
                {
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.HR, cancellationToken);
                    notMentorUser = new User(
                        SeedIds.SwaggerNotMentorUserId,
                        userCode,
                        notMentorEmail,
                        "[SWAGGER-UNHAPPY] Same Enterprise Non-Mentor",
                        UserRole.HR,
                        passHash);
                    notMentorUser.UpdateProfile(
                        notMentorUser.FullName,
                        GetUniquePhone(),
                        null,
                        UserGender.Male,
                        new DateOnly(1991, 3, 3),
                        fptEnterprise.Address);
                    notMentorUser.SetStatus(UserStatus.Active);
                    _context.Users.Add(notMentorUser);
                    existingEmails.Add(notMentorEmail);
                }

                bool hasNotMentorEu = await _context.EnterpriseUsers
                    .AnyAsync(eu => eu.UserId == notMentorUser.UserId && eu.EnterpriseId == fptEnterprise.EnterpriseId, cancellationToken);
                if (!hasNotMentorEu)
                {
                    var notMentorEu = new EnterpriseUser
                    {
                        EnterpriseUserId = SeedIds.SwaggerNotMentorEuId,
                        UserId = notMentorUser.UserId,
                        EnterpriseId = fptEnterprise.EnterpriseId
                    };
                    notMentorEu.UpdateMetadata("[SWAGGER-UNHAPPY] Role is HR, not Mentor", null, null);
                    _context.EnterpriseUsers.Add(notMentorEu);
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
                    user.UpdateProfile(user.FullName, GetUniquePhone(), null, UserGender.Male, new DateOnly(1985, 1, 1), uni.Address);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(uniAdminEmail);
                    var schoolAdminUu = new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user.UserId, UniversityId = uni.UniversityId };
                    schoolAdminUu.UpdateMetadata("School Administrator", null, null);
                    _context.UniversityUsers.Add(schoolAdminUu);

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
                    user.UpdateProfile(user.FullName, GetUniquePhone(), null, gender, new DateOnly(2004, 1, 1), "Hà Nội");
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
                user.UpdateProfile(user.FullName, GetUniquePhone(), null, UserGender.Male, new DateOnly(2002, 1, 1), "Hà Nội");
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

            // Student11 — seeded for UniAssign placement flow
            var student11Email = "student11@fptu.edu.vn";
            if (!existingEmails.Contains(student11Email))
            {
                var userId11 = SeedIds.Student11UserId;
                var userCode11 = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                var user11 = new User(userId11, userCode11, student11Email, "Lý Thị Mai", UserRole.Student, passHash);
                user11.UpdateProfile(user11.FullName, GetUniquePhone(), null, UserGender.Female, new DateOnly(2003, 5, 15), "Hà Nội");
                user11.SetStatus(UserStatus.Active);
                _context.Users.Add(user11);
                existingEmails.Add(student11Email);
                var uni11 = universityList.First(u => u.Code == "FPTU");
                _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user11.UserId, UniversityId = uni11.UniversityId });
                _context.Students.Add(new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user11.UserId,
                    InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS,
                    Major = "Information Technology",
                    ClassName = "IT1620"
                });
            }

            // Student12 — seeded for SelfApply placement flow (has CV)
            var student12Email = "student12@fptu.edu.vn";
            if (!existingEmails.Contains(student12Email))
            {
                var userId12 = SeedIds.Student12UserId;
                var userCode12 = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                var user12 = new User(userId12, userCode12, student12Email, "Phan Văn Khoa", UserRole.Student, passHash);
                user12.UpdateProfile(user12.FullName, GetUniquePhone(), null, UserGender.Male, new DateOnly(2003, 8, 20), "TP. Hồ Chí Minh");
                user12.SetStatus(UserStatus.Active);
                _context.Users.Add(user12);
                existingEmails.Add(student12Email);
                var uni12 = universityList.First(u => u.Code == "FPTU");
                _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user12.UserId, UniversityId = uni12.UniversityId });
                var student12Rec = new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user12.UserId,
                    InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS,
                    Major = "Software Engineering",
                    ClassName = "SE1621"
                };
                student12Rec.UpdateCv("https://iocv2-test-resources.s3.amazonaws.com/resumes/student12_cv.pdf");
                _context.Students.Add(student12Rec);
            }

            // Student13 — seeded for UniAssign placement flow
            var student13Email = "student13@fptu.edu.vn";
            if (!existingEmails.Contains(student13Email))
            {
                var userId13 = SeedIds.Student13UserId;
                var userCode13 = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                var user13 = new User(userId13, userCode13, student13Email, "Võ Thành Trung", UserRole.Student, passHash);
                user13.UpdateProfile(user13.FullName, GetUniquePhone(), null, UserGender.Male, new DateOnly(2003, 11, 10), "Hà Nội");
                user13.SetStatus(UserStatus.Active);
                _context.Users.Add(user13);
                existingEmails.Add(student13Email);
                var uni13 = universityList.First(u => u.Code == "FPTU");
                _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user13.UserId, UniversityId = uni13.UniversityId });
                _context.Students.Add(new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user13.UserId,
                    InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS,
                    Major = "Information Technology",
                    ClassName = "IT1621"
                });
            }

            // Student14 — seeded for SelfApply placement flow
            var student14Email = "student14@fptu.edu.vn";
            if (!existingEmails.Contains(student14Email))
            {
                var userId14 = SeedIds.Student14UserId;
                var userCode14 = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                var user14 = new User(userId14, userCode14, student14Email, "Nguyễn Thảo Ngân", UserRole.Student, passHash);
                user14.UpdateProfile(user14.FullName, GetUniquePhone(), null, UserGender.Female, new DateOnly(2003, 12, 12), "TP. Hồ Chí Minh");
                user14.SetStatus(UserStatus.Active);
                _context.Users.Add(user14);
                existingEmails.Add(student14Email);
                var uni14 = universityList.First(u => u.Code == "FPTU");
                _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user14.UserId, UniversityId = uni14.UniversityId });
                var student14Rec = new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user14.UserId,
                    InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS,
                    Major = "Software Engineering",
                    ClassName = "SE1622"
                };
                student14Rec.UpdateCv("https://iocv2-test-resources.s3.amazonaws.com/resumes/student14_cv.pdf");
                _context.Students.Add(student14Rec);
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

                var phase = InternshipPhase.Create(enterpriseId, name, start, end, majorFields, capacity, description, targetStatus);

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
                InternshipPhaseStatus.Open);

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

            var zeroMemberGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software Zero Member Team");
            if (zeroMemberGroup == null)
            {
                zeroMemberGroup = InternshipGroup.Create(
                    phaseInProgressFpt.PhaseId,
                    "FPT Software Zero Member Team",
                    "Edge case group: active, no mentor, and no members",
                    fsoft.EnterpriseId,
                    null,
                    DateTime.UtcNow.AddDays(-9),
                    DateTime.UtcNow.AddMonths(1));
                zeroMemberGroup.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(zeroMemberGroup);
            }

            var multiProjectGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software Multi Project Team");
            if (multiProjectGroup == null)
            {
                multiProjectGroup = InternshipGroup.Create(
                    phaseInProgressFpt.PhaseId,
                    "FPT Software Multi Project Team",
                    "Active team with multiple active projects to validate mentor cascade update",
                    fsoft.EnterpriseId,
                    mentorFptEuId,
                    DateTime.UtcNow.AddDays(-18),
                    DateTime.UtcNow.AddMonths(2));
                multiProjectGroup.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(multiProjectGroup);
            }

            await _context.SaveChangesAsync();

            var fptBackendJob = await _context.Jobs.FirstOrDefaultAsync(j => j.Title == "FPT Backend Platform Intern");
            var fptQaJob = await _context.Jobs.FirstOrDefaultAsync(j => j.Title == "FPT QA Automation Intern");
            var rikkeiBackendJob = await _context.Jobs.FirstOrDefaultAsync(j => j.Title == "Rikkeisoft Java Backend Intern");
            var rikkeiMobileJob = await _context.Jobs.FirstOrDefaultAsync(j => j.Title == "Rikkeisoft Mobile Flutter Intern");

            if (fptBackendJob == null || fptQaJob == null || rikkeiBackendJob == null || rikkeiMobileJob == null)
                return;

            var students = await _context.Students
                .Include(s => s.User)
                .Where(s => s.User.Email.StartsWith("student") && s.User.Email.EndsWith("@fptu.edu.vn"))
                .ToDictionaryAsync(s => s.User.Email);

            if (!students.TryGetValue("student1@fptu.edu.vn", out var s1) ||
                !students.TryGetValue("student2@fptu.edu.vn", out var s2) ||
                !students.TryGetValue("student3@fptu.edu.vn", out var s3) ||
                !students.TryGetValue("student4@fptu.edu.vn", out var s4) ||
                !students.TryGetValue("student5@fptu.edu.vn", out var s5) ||
                !students.TryGetValue("student6@fptu.edu.vn", out var s6) ||
                !students.TryGetValue("student7@fptu.edu.vn", out var s7) ||
                !students.TryGetValue("student8@fptu.edu.vn", out var s8) ||
                !students.TryGetValue("student9@fptu.edu.vn", out var s9) ||
                !students.TryGetValue("student10@fptu.edu.vn", out var s10))
            {
                return;
            }

            var fptUniversity = await _context.Universities.FirstOrDefaultAsync(u => u.Code == "FPTU");
            if (fptUniversity == null)
                return;

            students.TryGetValue("student11@fptu.edu.vn", out var s11);
            students.TryGetValue("student12@fptu.edu.vn", out var s12);
            students.TryGetValue("student13@fptu.edu.vn", out var s13);
            students.TryGetValue("student14@fptu.edu.vn", out var s14);

            var hrFptEu = await _context.EnterpriseUsers
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(eu => eu.EnterpriseId == fsoft.EnterpriseId && eu.User.Role == UserRole.HR);
            var hrRikkeiEu = await _context.EnterpriseUsers
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(eu => eu.EnterpriseId == rikkeisoft.EnterpriseId && eu.User.Role == UserRole.HR);

            async Task EnsureApplication(
                Guid enterpriseId,
                Guid termId,
                Guid studentId,
                Guid? jobId,
                InternshipApplicationStatus status,
                ApplicationSource source,
                DateTime appliedAt,
                DateTime? reviewedAt,
                Guid? reviewedBy,
                Guid? universityId,
                string? rejectReason,
                string? cvSnapshotUrl,
                string? jobPostingTitle,
                bool isHiddenByStudent = false)
            {
                var application = await _context.InternshipApplications.FirstOrDefaultAsync(a =>
                    a.EnterpriseId == enterpriseId &&
                    a.TermId == termId &&
                    a.StudentId == studentId &&
                    a.Source == source &&
                    a.JobId == jobId);

                if (application == null)
                {
                    application = new InternshipApplication { ApplicationId = Guid.NewGuid() };
                    _context.InternshipApplications.Add(application);
                }

                application.EnterpriseId = enterpriseId;
                application.TermId = termId;
                application.StudentId = studentId;
                application.JobId = jobId;
                application.Status = status;
                application.Source = source;
                application.AppliedAt = appliedAt;
                application.ReviewedAt = reviewedAt;
                application.ReviewedBy = reviewedBy;
                application.UniversityId = universityId;
                application.RejectReason = rejectReason;
                application.CvSnapshotUrl = cvSnapshotUrl;
                application.JobPostingTitle = jobPostingTitle;
                application.IsHiddenByStudent = isHiddenByStudent;
            }

            // FPT Spring 2026: placed from BOTH flows + returned capacity scenarios.
            await EnsureApplication(
                fsoft.EnterpriseId, spring2026.TermId, s1.StudentId,
                fptBackendJob.JobId, InternshipApplicationStatus.Placed, ApplicationSource.SelfApply,
                DateTime.UtcNow.AddDays(-35), DateTime.UtcNow.AddDays(-30), hrFptEu?.EnterpriseUserId,
                null, null,
                "https://iocv2-test-resources.s3.amazonaws.com/resumes/student1_cv.pdf", fptBackendJob.Title);

            await EnsureApplication(
                fsoft.EnterpriseId, spring2026.TermId, s2.StudentId,
                fptQaJob.JobId, InternshipApplicationStatus.Placed, ApplicationSource.UniAssign,
                DateTime.UtcNow.AddDays(-26), DateTime.UtcNow.AddDays(-24), hrFptEu?.EnterpriseUserId,
                fptUniversity.UniversityId, null,
                null, fptQaJob.Title);

            await EnsureApplication(
                fsoft.EnterpriseId, spring2026.TermId, s3.StudentId,
                fptBackendJob.JobId, InternshipApplicationStatus.Placed, ApplicationSource.SelfApply,
                DateTime.UtcNow.AddDays(-22), DateTime.UtcNow.AddDays(-20), hrFptEu?.EnterpriseUserId,
                null, null,
                "https://iocv2-test-resources.s3.amazonaws.com/resumes/student3_cv.pdf", fptBackendJob.Title);

            await EnsureApplication(
                fsoft.EnterpriseId, spring2026.TermId, s6.StudentId,
                fptQaJob.JobId, InternshipApplicationStatus.Placed, ApplicationSource.UniAssign,
                DateTime.UtcNow.AddDays(-16), DateTime.UtcNow.AddDays(-15), hrFptEu?.EnterpriseUserId,
                fptUniversity.UniversityId, null,
                null, fptQaJob.Title);

            await EnsureApplication(
                fsoft.EnterpriseId, spring2026.TermId, s4.StudentId,
                fptBackendJob.JobId, InternshipApplicationStatus.Rejected, ApplicationSource.SelfApply,
                DateTime.UtcNow.AddDays(-12), DateTime.UtcNow.AddDays(-10), hrFptEu?.EnterpriseUserId,
                null, "Không phù hợp stack backend của đợt này.",
                "https://iocv2-test-resources.s3.amazonaws.com/resumes/student4_cv.pdf", fptBackendJob.Title);

            await EnsureApplication(
                rikkeisoft.EnterpriseId, spring2026.TermId, s5.StudentId,
                rikkeiBackendJob.JobId, InternshipApplicationStatus.Placed, ApplicationSource.UniAssign,
                DateTime.UtcNow.AddDays(-18), DateTime.UtcNow.AddDays(-15), hrRikkeiEu?.EnterpriseUserId,
                fptUniversity.UniversityId, null,
                null, rikkeiBackendJob.Title);

            await EnsureApplication(
                fsoft.EnterpriseId, spring2026.TermId, s8.StudentId,
                fptBackendJob.JobId, InternshipApplicationStatus.Withdrawn, ApplicationSource.SelfApply,
                DateTime.UtcNow.AddDays(-14), DateTime.UtcNow.AddDays(-7), hrFptEu?.EnterpriseUserId,
                null, "Sinh viên rút hồ sơ theo nguyện vọng cá nhân.",
                "https://iocv2-test-resources.s3.amazonaws.com/resumes/student8_cv.pdf", fptBackendJob.Title,
                isHiddenByStudent: true);

            await EnsureApplication(
                fsoft.EnterpriseId, spring2026.TermId, s9.StudentId,
                fptQaJob.JobId, InternshipApplicationStatus.Applied, ApplicationSource.SelfApply,
                DateTime.UtcNow.AddDays(-3), null, null,
                null, null,
                "https://iocv2-test-resources.s3.amazonaws.com/resumes/student9_cv.pdf", fptQaJob.Title);

            // Rikkeisoft Spring 2026: mix of self-apply and uni-assign placements.
            await EnsureApplication(
                rikkeisoft.EnterpriseId, spring2026.TermId, s7.StudentId,
                rikkeiBackendJob.JobId, InternshipApplicationStatus.Placed, ApplicationSource.SelfApply,
                DateTime.UtcNow.AddDays(-18), DateTime.UtcNow.AddDays(-16), hrRikkeiEu?.EnterpriseUserId,
                null, null,
                "https://iocv2-test-resources.s3.amazonaws.com/resumes/student7_cv.pdf", rikkeiBackendJob.Title);

            await EnsureApplication(
                rikkeisoft.EnterpriseId, spring2026.TermId, s10.StudentId,
                rikkeiMobileJob.JobId, InternshipApplicationStatus.Placed, ApplicationSource.UniAssign,
                DateTime.UtcNow.AddDays(-11), DateTime.UtcNow.AddDays(-8), hrRikkeiEu?.EnterpriseUserId,
                fptUniversity.UniversityId, null,
                null, rikkeiMobileJob.Title);

            await EnsureApplication(
                rikkeisoft.EnterpriseId, spring2026.TermId, s2.StudentId,
                rikkeiMobileJob.JobId, InternshipApplicationStatus.PendingAssignment, ApplicationSource.UniAssign,
                DateTime.UtcNow.AddDays(-5), null, null,
                fptUniversity.UniversityId, null,
                null, rikkeiMobileJob.Title);

            // Student 11: UniAssign placement flow -> FPT Software Spring 2026 (Job: FPT QA Automation Intern)
            if (s11 != null)
            {
                await EnsureApplication(
                   fsoft.EnterpriseId, spring2026.TermId, s11.StudentId,
                   fptQaJob.JobId, InternshipApplicationStatus.PendingAssignment, ApplicationSource.UniAssign,
                   DateTime.UtcNow.AddDays(-4), null, null,
                   fptUniversity.UniversityId, null,
                   null, fptQaJob.Title);
            }

            // Student 12: SelfApply flow -> FPT Software Spring 2026 (Job: FPT Backend Platform Intern)
            if (s12 != null)
            {
                await EnsureApplication(
                   fsoft.EnterpriseId, spring2026.TermId, s12.StudentId,
                   fptBackendJob.JobId, InternshipApplicationStatus.Applied, ApplicationSource.SelfApply,
                   DateTime.UtcNow.AddDays(-2), null, null,
                   null, null,
                   "https://iocv2-test-resources.s3.amazonaws.com/resumes/student12_cv.pdf", fptBackendJob.Title);
            }

            // Student 13: UniAssign placement flow -> Rikkeisoft Spring 2026 (Job: Rikkeisoft Java Backend Intern)
            if (s13 != null)
            {
                await EnsureApplication(
                   rikkeisoft.EnterpriseId, spring2026.TermId, s13.StudentId,
                   rikkeiBackendJob.JobId, InternshipApplicationStatus.PendingAssignment, ApplicationSource.UniAssign,
                   DateTime.UtcNow.AddDays(-1), null, null,
                   fptUniversity.UniversityId, null,
                   null, rikkeiBackendJob.Title);
            }

            // Student 14: SelfApply flow -> Rikkeisoft Spring 2026 (Job: Rikkeisoft Mobile Flutter Intern)
            if (s14 != null)
            {
                await EnsureApplication(
                   rikkeisoft.EnterpriseId, spring2026.TermId, s14.StudentId,
                   rikkeiMobileJob.JobId, InternshipApplicationStatus.Applied, ApplicationSource.SelfApply,
                   DateTime.UtcNow.AddDays(0), null, null,
                   null, null,
                   "https://iocv2-test-resources.s3.amazonaws.com/resumes/student14_cv.pdf", rikkeiMobileJob.Title);
            }

            // Historical record for old term.
            await EnsureApplication(
                fsoft.EnterpriseId, fall2025.TermId, s2.StudentId,
                fptBackendJob.JobId, InternshipApplicationStatus.Rejected, ApplicationSource.SelfApply,
                DateTime.UtcNow.AddDays(-100), DateTime.UtcNow.AddDays(-95), hrFptEu?.EnterpriseUserId,
                null, "Not a good fit for this semester",
                "https://iocv2-test-resources.s3.amazonaws.com/resumes/student2_cv.pdf", fptBackendJob.Title);

            await _context.SaveChangesAsync();
        }

        private async Task SeedProjectsAndWorkItems()
        {
            // Guard by stable project codes — không dùng AnyAsync() thuần vì có thể bị soft-delete filter
            if (await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-FPTSOF_FPT_1")) return;

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
            if (!await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-FPTSOF_FPT_2"))
            {
                _context.Projects.Add(projPending);

                var cancelledSprint = new Sprint(projPending.ProjectId, "Cancelled Sprint", "A sprint that was planned but cancelled");
                cancelledSprint.Start(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)), DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)));
                _context.Sprints.Add(cancelledSprint);

                _context.WorkItems.AddRange(
                    new WorkItem { WorkItemId = Guid.NewGuid(), ProjectId = projPending.ProjectId, Title = "Gather Requirements", Type = WorkItemType.Task, Status = WorkItemStatus.Todo, AssigneeId = null, DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(15)) },
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
            // ⚠️ Guard bằng ProjectCode (stable key) thay vì InternshipId (thay đổi khi DB re-seed).
            // Lý do: InternshipGroup.Create() sinh Guid mới mỗi lần insert → nếu groups bị xóa/tạo lại,
            // InternshipId thay đổi → guard AnyAsync(p.InternshipId == group.InternshipId) miss →
            // cố insert code đã tồn tại → vi phạm uix_projects_project_code_active.

            var zeroMemberGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software Zero Member Team");
            if (zeroMemberGroup != null && !await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-FPTSOF_FPT_7"))
            {
                var zeroMemberProj = Project.Create(
                    "FPT Zero Member Automation",
                    "Edge scenario project for group without members",
                    "PRJ-FPTSOF_FPT_7",
                    "Automation",
                    "Validate assign mentor still updates project even when member list is empty.");
                zeroMemberProj.AssignToGroup(zeroMemberGroup.InternshipId, DateTime.UtcNow.AddDays(-8), DateTime.UtcNow.AddMonths(1));
                zeroMemberProj.Publish();
                _context.Projects.Add(zeroMemberProj);
            }

            var multiProjectGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software Multi Project Team");
            if (multiProjectGroup != null && !await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-FPTSOF_FPT_8"))
            {
                var multiProj1 = Project.Create(
                    "FPT Mentor Rotation Platform",
                    "Primary API project used for mentor reassign project-sync tests",
                    "PRJ-FPTSOF_FPT_8",
                    "CNTT",
                    "Back-end platform with active sprint cadence.",
                    mentorId: SeedIds.MentorFptEuId);
                multiProj1.AssignToGroup(multiProjectGroup.InternshipId, DateTime.UtcNow.AddDays(-16), DateTime.UtcNow.AddMonths(2));
                multiProj1.Publish();
                _context.Projects.Add(multiProj1);
            }

            if (!await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-FPTSOF_FPT_9"))
            {
                var multiProj2 = Project.Create(
                    "FPT Mentor Rotation Analytics",
                    "Secondary analytics project in same group to verify bulk mentor update",
                    "PRJ-FPTSOF_FPT_9",
                    "Data",
                    "Analytics and reporting track for internship outcomes.",
                    mentorId: SeedIds.MentorFptEuId);
                multiProj2.Publish();
                _context.Projects.Add(multiProj2);
            }

            var rikkeiGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");
            if (rikkeiGroup != null && !await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-RIKKES_RIKK_2"))
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
            }

            if (!await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-RIKKES_RIKK_3"))
            {
                var rikkeiProj2 = Project.Create(
                    "Rikkeisoft Mobile App",
                    "Ứng dụng di động cho khách hàng của Rikkeisoft",
                    "PRJ-RIKKES_RIKK_3",
                    "Mobile",
                    "Phát triển ứng dụng mobile cross-platform bằng Flutter.",
                    mentorId: SeedIds.MentorRikkeisoftEuId);
                _context.Projects.Add(rikkeiProj2);
            }

            var fptCtGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software CT OJT Team");
            if (fptCtGroup != null && !await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-FPTSOF_FPT_3"))
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
            if (archivedGroup != null && !await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-FPTSOF_FPT_4"))
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

            if (!await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-ORPHAN_001"))
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

            if (!await _context.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == "PRJ-FPTSOF_FPT_5"))
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
            var s11 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student11@fptu.edu.vn");
            var s12 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student12@fptu.edu.vn");

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
            var s12 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student12@fptu.edu.vn");

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
            var fptStudentIds = new[] { s1.StudentId, s2.StudentId, s3.StudentId, s6.StudentId };
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
            var fptJoinedAt = DateTime.UtcNow.AddDays(-28);
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

                var fptId = fptGroup.InternshipId;
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
                    CycleId = Guid.NewGuid(),
                    PhaseId = phaseFptSpring.PhaseId,
                    Name = "Final Evaluation Spring 2026",
                    StartDate = DateTime.UtcNow.AddDays(5),
                    EndDate = DateTime.UtcNow.AddDays(25),
                    Status = EvaluationCycleStatus.Grading
                };
                finalCycle.Criteria.Add(new EvaluationCriteria
                {
                    CriteriaId = Guid.NewGuid(),
                    CycleId = finalCycle.CycleId,
                    Name = "Technical Skills",
                    Description = "Code quality, architecture, and problem-solving",
                    MaxScore = 10m,
                    Weight = 0.60m
                });
                finalCycle.Criteria.Add(new EvaluationCriteria
                {
                    CriteriaId = Guid.NewGuid(),
                    CycleId = finalCycle.CycleId,
                    Name = "Soft Skills",
                    Description = "Communication, teamwork, and attitude",
                    MaxScore = 10m,
                    Weight = 0.40m
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
                    CycleId = Guid.NewGuid(),
                    PhaseId = phaseRikkeiSpring.PhaseId,
                    Name = "Mid-term Rikkeisoft Spring 2026",
                    StartDate = DateTime.UtcNow.AddDays(-8),
                    EndDate = DateTime.UtcNow.AddDays(12),
                    Status = EvaluationCycleStatus.Grading
                };
                rikkeiMidCycle.Criteria.Add(new EvaluationCriteria
                {
                    CriteriaId = Guid.NewGuid(),
                    CycleId = rikkeiMidCycle.CycleId,
                    Name = "Technical Skills",
                    Description = "Backend development skills and code quality",
                    MaxScore = 10m,
                    Weight = 0.60m
                });
                rikkeiMidCycle.Criteria.Add(new EvaluationCriteria
                {
                    CriteriaId = Guid.NewGuid(),
                    CycleId = rikkeiMidCycle.CycleId,
                    Name = "Professional Skills",
                    Description = "Professionalism, punctuality, and collaboration",
                    MaxScore = 10m,
                    Weight = 0.40m
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
                    EvaluationId = Guid.NewGuid(),
                    CycleId = cycleId,
                    InternshipId = internshipId,
                    StudentId = studentId,
                    EvaluatorId = evaluatorId,
                    Status = EvaluationStatus.Published,
                    Note = note
                };
                decimal total = 0;
                foreach (var (criteriaId, score, weight, comment) in details)
                {
                    eval.Details.Add(new EvaluationDetail
                    {
                        DetailId = Guid.NewGuid(),
                        EvaluationId = eval.EvaluationId,
                        CriteriaId = criteriaId,
                        Score = score,
                        Comment = comment
                    });
                    total += score * weight;
                }
                eval.TotalScore = Math.Round(total, 2);
                _context.Set<Evaluation>().Add(eval);
            }

            var midCriteria = midTermCycle.Criteria.OrderBy(c => c.Name).ToList(); // Soft Skills, Technical Skills
            var finalCriteria = finalCycle.Criteria.OrderBy(c => c.Name).ToList();
            var rikkeiCriteria = rikkeiMidCycle.Criteria.OrderBy(c => c.Name).ToList();

            // Tìm Technical và Soft criteria cho từng cycle
            var midTech = midCriteria.FirstOrDefault(c => c.Name.Contains("Technical"));
            var midSoft = midCriteria.FirstOrDefault(c => c.Name.Contains("Soft") || c.Name.Contains("Soft"));
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
                    CycleId = rikkeiMidCycle.CycleId,
                    InternshipId = rikkeiGroup.InternshipId,
                    StudentId = s5.StudentId,
                    EvaluatorId = mentorRikkei.UserId,
                    Status = EvaluationStatus.Draft,
                    Note = "Em cần cải thiện nhiều hơn về chuyên cần.",
                    TotalScore = null
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
                        StudentId = s5.StudentId,
                        InternshipGroupId = rikkeiGroup.InternshipId,
                        OccurredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-12)),
                        Description = "Vắng buổi weekly meeting không thông báo trước.",
                        CreatedBy = mentorRikkei.UserId,
                        CreatedAt = DateTime.UtcNow.AddDays(-11)
                    },
                    new ViolationReport
                    {
                        ViolationReportId = Guid.NewGuid(),
                        StudentId = s5.StudentId,
                        InternshipGroupId = rikkeiGroup.InternshipId,
                        OccurredDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
                        Description = "Nộp task trễ deadline 2 ngày liên tiếp mà không có báo cáo.",
                        CreatedBy = mentorRikkei.UserId,
                        CreatedAt = DateTime.UtcNow.AddDays(-4)
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

        /// <summary>
        /// Seed đầy đủ dữ liệu cho demo flow GROUP &amp; ASSIGNMENT (HR) — 5 bước.
        ///
        /// ── Sinh viên (7 người, tất cả Placed tại FPT Software, Summer 2026 term) ────
        ///   A  Trần Minh Đức      SE1625  SelfApply   → thành viên ban đầu khi Tạo Group, bị Remove bước 4
        ///   B  Nguyễn Thị Hương   IT1623  UniAssign   → thành viên ban đầu khi Tạo Group, ở lại đến cuối
        ///   C  Lê Quốc Bảo        SE1626  SelfApply   → free, dùng cho Add Student bước 3
        ///   D  Phạm Thị Lan       CS1624  UniAssign   → pool dự phòng, xuất hiện trong dropdown
        ///   E  Hoàng Văn Minh     SE1627  SelfApply   → thành viên pre-seeded group (Leader)
        ///   F  Võ Thị Ngọc        IT1626  UniAssign   → thành viên pre-seeded group (Member)
        ///   G  Đặng Quang Hùng    SE1628  SelfApply   → pool dự phòng
        ///
        /// ── Pre-seeded group ──────────────────────────────────────────────────────────
        ///   "FPT Demo Presentation Group"
        ///   → Phase: Summer 2026 (Open), Status: Active, Mentor: null
        ///   → Thành viên: E (Leader) + F (Member)
        ///   → QUAN TRỌNG: Dùng E+F để A/B/C/D/G hoàn toàn free cho demo tạo group mới
        ///   → Dùng trực tiếp cho bước 2–5 nếu không muốn demo Tạo Group từ đầu
        ///
        /// ── Điều kiện happy case đã được đảm bảo ─────────────────────────────────────
        ///   ✓ Tất cả 15 sv có Application Status=Placed tại FPT Software, Summer 2026 term
        ///   ✓ Tất cả 15 sv KHÔNG nằm trong bất kỳ nhóm Active nào → hoàn toàn free cho demo flow
        ///   ✓ Pre-seeded group tồn tại nhưng KHÔNG có thành viên từ pool demo (không gây conflict)
        ///   ✓ Phase "FPT Software Summer 2026" = Open (xem SeedInternshipPhases)
        ///   ✓ Term "Summer 2026" = Open (xem SeedTerms) — khớp với phase
        ///   ✓ mentor@fptsoftware.com + mentor.backup@fptsoftware.com = Mentor, Active, cùng enterprise
        ///   ✓ hr@fptsoftware.com = HR, Active, cùng enterprise
        ///
        /// ── Tài khoản demo ────────────────────────────────────────────────────────────
        ///   HR      : hr@fptsoftware.com               / Admin@123
        ///   Mentor  : mentor@fptsoftware.com            / Admin@123  (Assign bước 5)
        ///   Mentor 2: mentor.backup@fptsoftware.com     / Admin@123  (Reassign nếu cần)
        ///
        /// ── Hướng dẫn demo 5 bước ────────────────────────────────────────────────────
        ///   Bước 1 — Tạo Group   : Phase=Summer 2026, chọn A (Leader) + B (Member)
        ///   Bước 2 — Update Group: Đổi tên/mô tả group vừa tạo
        ///   Bước 3 — Add Student : Thêm C vào group (C free, Placed, không ở group nào)
        ///   Bước 4 — Remove      : Xóa A ra khỏi group
        ///   Bước 5 — Assign Mentor: Chọn mentor@fptsoftware.com
        /// </summary>
        private async Task SeedDemoGroupAssignmentStudents()
        {
            const string seedMarker = "demo.student.a@fptu.edu.vn";
            if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == seedMarker))
                return;

            var fptu = await _context.Universities.FirstOrDefaultAsync(u => u.Code == "FPTU");
            if (fptu == null) return;

            var fsoft = await _context.Enterprises.FirstOrDefaultAsync(e => e.Name == "FPT Software");
            if (fsoft == null) return;

            // Phase Summer 2026 (Open) — target phase cho demo group creation
            var phaseSummer2026 = await _context.InternshipPhases.FirstOrDefaultAsync(
                p => p.EnterpriseId == fsoft.EnterpriseId && p.Name == "FPT Software Summer 2026");
            if (phaseSummer2026 == null) return;

            // ⚠️ FIX: Dùng Summer 2026 term (khớp với Summer 2026 phase) thay vì Spring 2026
            // InternshipPhase (đợt tuyển của enterprise) ≠ Term (học kỳ của university)
            // nhưng cần khớp để data có nghĩa về mặt nghiệp vụ
            var summer2026Term = await _context.Terms.FirstOrDefaultAsync(
                t => t.UniversityId == fptu.UniversityId && t.Name == "Summer 2026");
            // Fallback sang Spring 2026 nếu Summer term chưa tồn tại
            var fallbackTerm = summer2026Term ?? await _context.Terms.FirstOrDefaultAsync(
                t => t.UniversityId == fptu.UniversityId && t.Name == "Spring 2026");
            if (fallbackTerm == null) return;

            var fptBackendJob = await _context.Jobs.FirstOrDefaultAsync(j => j.Title == "FPT Backend Platform Intern");
            var fptQaJob = await _context.Jobs.FirstOrDefaultAsync(j => j.Title == "FPT QA Automation Intern");

            var hrFptEu = await _context.EnterpriseUsers
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(eu => eu.EnterpriseId == fsoft.EnterpriseId && eu.User.Role == UserRole.HR);

            var passHash = _passwordService.HashPassword("Admin@123");
            CancellationToken ct = default;

            // ─────────────────────────────────────────────────────────────────────────
            // Helper nội bộ: tạo 1 student hoàn chỉnh
            //   User + Student + UniversityUser + StudentTerm + InternshipApplication(Placed)
            // ─────────────────────────────────────────────────────────────────────────
            async Task<Student> CreateDemoStudent(
                Guid userId, string email, string fullName,
                UserGender gender, DateOnly dob,
                string major, string className,
                ApplicationSource appSource,
                Guid? jobId, string jobTitle,
                int appliedDaysAgo)
            {
                var userCode = await _userService.GenerateUserCodeAsync(UserRole.Student, ct);
                var user = new User(userId, userCode, email, fullName, UserRole.Student, passHash);
                // Phone range 0912xxxxxx — tách biệt hoàn toàn với SeedUsers (098765xxxx)
                user.UpdateProfile(fullName, $"0912{appliedDaysAgo:D6}", null, gender, dob, "Hà Nội");
                user.SetStatus(UserStatus.Active);
                _context.Users.Add(user);

                var student = new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user.UserId,
                    InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS,
                    Major = major,
                    ClassName = className
                };
                _context.Students.Add(student);

                _context.UniversityUsers.Add(new UniversityUser
                {
                    UniversityUserId = Guid.NewGuid(),
                    UserId = user.UserId,
                    UniversityId = fptu.UniversityId
                });

                // StudentTerm — dùng Summer 2026 để khớp với group phase
                // SeedTerms() chỉ query email.StartsWith("student"), không đụng "demo.student.*"
                // nên không có nguy cơ duplicate StudentTerm
                _context.StudentTerms.Add(new StudentTerm
                {
                    StudentTermId = Guid.NewGuid(),
                    StudentId = student.StudentId,
                    TermId = fallbackTerm.TermId,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    PlacementStatus = PlacementStatus.Placed,
                    EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-appliedDaysAgo - 10))
                });

                // InternshipApplication — Status=Placed tại FPT Software
                // Điều kiện cốt lõi: handler check EnterpriseId + Status=Placed (không check TermId/PhaseId)
                _context.InternshipApplications.Add(new InternshipApplication
                {
                    ApplicationId = Guid.NewGuid(),
                    EnterpriseId = fsoft.EnterpriseId,
                    TermId = fallbackTerm.TermId,
                    StudentId = student.StudentId,
                    JobId = jobId,
                    Status = InternshipApplicationStatus.Placed,
                    Source = appSource,
                    AppliedAt = DateTime.UtcNow.AddDays(-appliedDaysAgo),
                    ReviewedAt = DateTime.UtcNow.AddDays(-appliedDaysAgo + 5),
                    ReviewedBy = hrFptEu?.EnterpriseUserId,
                    UniversityId = appSource == ApplicationSource.UniAssign ? fptu.UniversityId : null,
                    JobPostingTitle = jobTitle
                });

                return student;
            }

            // ── A: Trần Minh Đức — thành viên ban đầu bước 1, bị Remove ở bước 4 ─────
            var studentA = await CreateDemoStudent(
                SeedIds.DemoGroupStudentAUserId,
                "demo.student.a@fptu.edu.vn", "Trần Minh Đức",
                UserGender.Male, new DateOnly(2003, 3, 15),
                "Software Engineering", "SE1625",
                ApplicationSource.SelfApply,
                fptBackendJob?.JobId, fptBackendJob?.Title ?? "FPT Backend Platform Intern",
                appliedDaysAgo: 40);

            // ── B: Nguyễn Thị Hương — thành viên ban đầu bước 1, ở lại đến cuối ───────
            var studentB = await CreateDemoStudent(
                SeedIds.DemoGroupStudentBUserId,
                "demo.student.b@fptu.edu.vn", "Nguyễn Thị Hương",
                UserGender.Female, new DateOnly(2003, 7, 20),
                "Information Technology", "IT1623",
                ApplicationSource.UniAssign,
                fptQaJob?.JobId, fptQaJob?.Title ?? "FPT QA Automation Intern",
                appliedDaysAgo: 38);

            // ── C: Lê Quốc Bảo — free pool, dùng cho Add Student bước 3 ───────────────
            var studentC = await CreateDemoStudent(
                SeedIds.DemoGroupStudentCUserId,
                "demo.student.c@fptu.edu.vn", "Lê Quốc Bảo",
                UserGender.Male, new DateOnly(2003, 11, 5),
                "Software Engineering", "SE1626",
                ApplicationSource.SelfApply,
                fptBackendJob?.JobId, fptBackendJob?.Title ?? "FPT Backend Platform Intern",
                appliedDaysAgo: 36);

            // ── D: Phạm Thị Lan — pool dự phòng, xuất hiện trong dropdown ─────────────
            var studentD = await CreateDemoStudent(
                SeedIds.DemoGroupStudentDUserId,
                "demo.student.d@fptu.edu.vn", "Phạm Thị Lan",
                UserGender.Female, new DateOnly(2003, 5, 10),
                "Computer Science", "CS1624",
                ApplicationSource.UniAssign,
                fptQaJob?.JobId, fptQaJob?.Title ?? "FPT QA Automation Intern",
                appliedDaysAgo: 34);

            // ── E: Hoàng Văn Minh — pool dự phòng ───────────────────────────────────
            var studentE = await CreateDemoStudent(
                SeedIds.DemoGroupStudentEUserId,
                "demo.student.e@fptu.edu.vn", "Hoàng Văn Minh",
                UserGender.Male, new DateOnly(2003, 1, 25),
                "Software Engineering", "SE1627",
                ApplicationSource.SelfApply,
                fptBackendJob?.JobId, fptBackendJob?.Title ?? "FPT Backend Platform Intern",
                appliedDaysAgo: 32);

            // ── F: Võ Thị Ngọc — pool dự phòng ──────────────────────────────────────
            var studentF = await CreateDemoStudent(
                SeedIds.DemoGroupStudentFUserId,
                "demo.student.f@fptu.edu.vn", "Võ Thị Ngọc",
                UserGender.Female, new DateOnly(2003, 9, 3),
                "Information Technology", "IT1626",
                ApplicationSource.UniAssign,
                fptQaJob?.JobId, fptQaJob?.Title ?? "FPT QA Automation Intern",
                appliedDaysAgo: 30);

            // ── G: Đặng Quang Hùng — pool dự phòng ───────────────────────────────────
            var studentG = await CreateDemoStudent(
                SeedIds.DemoGroupStudentGUserId,
                "demo.student.g@fptu.edu.vn", "Đặng Quang Hùng",
                UserGender.Male, new DateOnly(2002, 12, 18),
                "Software Engineering", "SE1628",
                ApplicationSource.SelfApply,
                fptBackendJob?.JobId, fptBackendJob?.Title ?? "FPT Backend Platform Intern",
                appliedDaysAgo: 28);

            // ── H: Trương Thị Mai — pool dự phòng ────────────────────────────────────
            var studentH = await CreateDemoStudent(
                SeedIds.DemoGroupStudentHUserId,
                "demo.student.h@fptu.edu.vn", "Trương Thị Mai",
                UserGender.Female, new DateOnly(2003, 4, 8),
                "Computer Science", "CS1625",
                ApplicationSource.UniAssign,
                fptQaJob?.JobId, fptQaJob?.Title ?? "FPT QA Automation Intern",
                appliedDaysAgo: 26);

            // ── I: Bùi Văn Thành — pool dự phòng ─────────────────────────────────────
            var studentI = await CreateDemoStudent(
                SeedIds.DemoGroupStudentIUserId,
                "demo.student.i@fptu.edu.vn", "Bùi Văn Thành",
                UserGender.Male, new DateOnly(2003, 6, 22),
                "Software Engineering", "SE1629",
                ApplicationSource.SelfApply,
                fptBackendJob?.JobId, fptBackendJob?.Title ?? "FPT Backend Platform Intern",
                appliedDaysAgo: 24);

            // ── J: Ngô Thị Hà — pool dự phòng ────────────────────────────────────────
            var studentJ = await CreateDemoStudent(
                SeedIds.DemoGroupStudentJUserId,
                "demo.student.j@fptu.edu.vn", "Ngô Thị Hà",
                UserGender.Female, new DateOnly(2002, 10, 14),
                "Information Technology", "IT1627",
                ApplicationSource.UniAssign,
                fptQaJob?.JobId, fptQaJob?.Title ?? "FPT QA Automation Intern",
                appliedDaysAgo: 22);

            // ── K: Đinh Quang Khải — pool dự phòng ───────────────────────────────────
            var studentK = await CreateDemoStudent(
                SeedIds.DemoGroupStudentKUserId,
                "demo.student.k@fptu.edu.vn", "Đinh Quang Khải",
                UserGender.Male, new DateOnly(2003, 2, 27),
                "Software Engineering", "SE1630",
                ApplicationSource.SelfApply,
                fptBackendJob?.JobId, fptBackendJob?.Title ?? "FPT Backend Platform Intern",
                appliedDaysAgo: 20);

            // ── L: Lý Thị Thu — pool dự phòng ────────────────────────────────────────
            var studentL = await CreateDemoStudent(
                SeedIds.DemoGroupStudentLUserId,
                "demo.student.l@fptu.edu.vn", "Lý Thị Thu",
                UserGender.Female, new DateOnly(2003, 8, 11),
                "Computer Science", "CS1626",
                ApplicationSource.UniAssign,
                fptQaJob?.JobId, fptQaJob?.Title ?? "FPT QA Automation Intern",
                appliedDaysAgo: 18);

            // ── M: Vũ Minh Tuấn — pool dự phòng ──────────────────────────────────────
            var studentM = await CreateDemoStudent(
                SeedIds.DemoGroupStudentMUserId,
                "demo.student.m@fptu.edu.vn", "Vũ Minh Tuấn",
                UserGender.Male, new DateOnly(2002, 11, 3),
                "Software Engineering", "SE1631",
                ApplicationSource.SelfApply,
                fptBackendJob?.JobId, fptBackendJob?.Title ?? "FPT Backend Platform Intern",
                appliedDaysAgo: 16);

            // ── N: Trịnh Thị Dung — pool dự phòng ───────────────────────────────────
            var studentN = await CreateDemoStudent(
                SeedIds.DemoGroupStudentNUserId,
                "demo.student.n@fptu.edu.vn", "Trịnh Thị Dung",
                UserGender.Female, new DateOnly(2003, 1, 19),
                "Information Technology", "IT1628",
                ApplicationSource.UniAssign,
                fptQaJob?.JobId, fptQaJob?.Title ?? "FPT QA Automation Intern",
                appliedDaysAgo: 14);

            // ── O: Phùng Văn Long — pool dự phòng ────────────────────────────────────
            var studentO = await CreateDemoStudent(
                SeedIds.DemoGroupStudentOUserId,
                "demo.student.o@fptu.edu.vn", "Phùng Văn Long",
                UserGender.Male, new DateOnly(2002, 7, 30),
                "Software Engineering", "SE1632",
                ApplicationSource.SelfApply,
                fptBackendJob?.JobId, fptBackendJob?.Title ?? "FPT Backend Platform Intern",
                appliedDaysAgo: 12);

            await _context.SaveChangesAsync();

            // ─────────────────────────────────────────────────────────────────────────
            // Pre-seeded group: "FPT Demo Presentation Group"
            //   Active, chưa có Mentor, KHÔNG có thành viên từ pool demo (A–O)
            //   → Tất cả 15 sv hoàn toàn FREE cho demo flow
            //   → Group tồn tại để UI có dữ liệu nền (hiển thị trong danh sách nhóm)
            // ─────────────────────────────────────────────────────────────────────────
            var demoGroupExists = await _context.InternshipGroups
                .AnyAsync(g => g.GroupName == "FPT Demo Presentation Group");

            if (!demoGroupExists)
            {
                var demoGroup = InternshipGroup.Create(
                    phaseSummer2026.PhaseId,
                    "FPT Demo Presentation Group",
                    "Nhóm thực tập Summer 2026 — dành riêng cho demo báo cáo (Active, chưa có Mentor)",
                    fsoft.EnterpriseId,
                    mentorId: null,
                    DateTime.UtcNow.AddDays(-5),
                    DateTime.UtcNow.AddMonths(3));
                demoGroup.UpdateStatus(GroupStatus.Active);

                _context.InternshipGroups.Add(demoGroup);
                await _context.SaveChangesAsync();
            }
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


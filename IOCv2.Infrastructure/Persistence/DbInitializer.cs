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
            
            public static readonly List<Guid> StudentIds = new()
            {
                new Guid("66666666-6666-6666-6666-666666660001"),
                new Guid("66666666-6666-6666-6666-666666660002"),
                new Guid("66666666-6666-6666-6666-666666660003"),
                new Guid("66666666-6666-6666-6666-666666660004"),
                new Guid("66666666-6666-6666-6666-666666660005")
            };
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
            await SeedUsers();
            await SeedTerms();
            await SeedInternshipPhases();   // ← must be before SeedInternshipGroups
            await SeedInternshipGroups();
            await SeedProjectsAndWorkItems();
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

            // 1. Super Admin
            if (!await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Role == UserRole.SuperAdmin))
            {
                var userId = SeedIds.SuperAdminId;
                var userCode = await _userService.GenerateUserCodeAsync(UserRole.SuperAdmin, cancellationToken);
                var superAdmin = new User(userId, userCode, "admin@iocv2.com", "Super Administrator", UserRole.SuperAdmin, passHash);
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
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(adminEmail);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), UserId = user.UserId, EnterpriseId = ent.EnterpriseId, Position = "Enterprise Administrator" });
                }

                var mentorEmail = $"mentor@{baseName}.com";
                if (!existingEmails.Contains(mentorEmail))
                {
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Mentor, cancellationToken);
                    var user = new User(mentorId, userCode, mentorEmail, $"Mentor {ent.Name}", UserRole.Mentor, passHash);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(mentorEmail);
                    _context.EnterpriseUsers.Add(new EnterpriseUser { EnterpriseUserId = Guid.NewGuid(), UserId = user.UserId, EnterpriseId = ent.EnterpriseId, Position = "Technical Mentor" });
                }

                // HR account (new)
                var hrEmail = $"hr@{baseName}.com";
                if (!existingEmails.Contains(hrEmail))
                {
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.HR, cancellationToken);
                    var user = new User(hrId, userCode, hrEmail, $"HR {ent.Name}", UserRole.HR, passHash);
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
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(uniAdminEmail);
                    _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user.UserId, UniversityId = uni.UniversityId, Position = "School Administrator" });
                }
            }

            // 4. 5 Specific Students
            string[] studentEmails = {
                "student1@fptu.edu.vn",
                "student2@fptu.edu.vn",
                "student3@fptu.edu.vn",
                "student4@fptu.edu.vn",
                "student5@fptu.edu.vn"
            };

            string[] studentNames = { "Nguyễn Văn Một", "Trần Thị Hai", "Lê Văn Ba", "Phạm Thị Bốn", "Hoàng Văn Năm" };
            StudentStatus[] studentStatuses = { StudentStatus.NO_INTERNSHIP, StudentStatus.APPLIED, StudentStatus.INTERNSHIP_IN_PROGRESS, StudentStatus.APPLIED, StudentStatus.COMPLETED };

            for (int i = 0; i < studentEmails.Length; i++)
            {
                if (!existingEmails.Contains(studentEmails[i]))
                {
                    var userId = SeedIds.StudentIds[i];
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                    var user = new User(userId, userCode, studentEmails[i], studentNames[i], UserRole.Student, passHash);
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(studentEmails[i]);

                    var uni = universityList.First(u => u.Code == "FPTU");
                    _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user.UserId, UniversityId = uni.UniversityId });
                    _context.Students.Add(new Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = user.UserId,
                        InternshipStatus = studentStatuses[i],
                        Major = "Software Engineering",
                        ClassName = "SE1616"
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

            // Enrollment for students used by active-term and application test cases
            var s2 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s4 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");

            if (!await _context.StudentTerms.AnyAsync(st => st.StudentId == s2.StudentId && st.TermId == spring2026.TermId))
            {
                _context.StudentTerms.Add(new StudentTerm
                {
                    StudentTermId = Guid.NewGuid(),
                    StudentId = s2.StudentId,
                    TermId = spring2026.TermId,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    PlacementStatus = PlacementStatus.Unplaced,
                    EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60))
                });
            }

            if (!await _context.StudentTerms.AnyAsync(st => st.StudentId == s3.StudentId && st.TermId == spring2026.TermId))
            {
                _context.StudentTerms.Add(new StudentTerm
                {
                    StudentTermId = Guid.NewGuid(),
                    StudentId = s3.StudentId,
                    TermId = spring2026.TermId,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    PlacementStatus = PlacementStatus.Placed,
                    EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60))
                });
            }

            if (!await _context.StudentTerms.AnyAsync(st => st.StudentId == s4.StudentId && st.TermId == summer2026.TermId))
            {
                _context.StudentTerms.Add(new StudentTerm
                {
                    StudentTermId = Guid.NewGuid(),
                    StudentId = s4.StudentId,
                    TermId = summer2026.TermId,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    PlacementStatus = PlacementStatus.Unplaced,
                    EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))
                });
            }

            if (!await _context.StudentTerms.AnyAsync(st => st.StudentId == s4.StudentId && st.TermId == spring2026Ct.TermId))
            {
                _context.StudentTerms.Add(new StudentTerm
                {
                    StudentTermId = Guid.NewGuid(),
                    StudentId = s4.StudentId,
                    TermId = spring2026Ct.TermId,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    PlacementStatus = PlacementStatus.Placed,
                    EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30))
                });
            }

            if (!await _context.StudentTerms.AnyAsync(st => st.StudentId == s5.StudentId && st.TermId == fall2025.TermId))
            {
                _context.StudentTerms.Add(new StudentTerm
                {
                    StudentTermId = Guid.NewGuid(),
                    StudentId = s5.StudentId,
                    TermId = fall2025.TermId,
                    EnrollmentStatus = EnrollmentStatus.Active,
                    PlacementStatus = PlacementStatus.Placed,
                    EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-200))
                });
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedInternshipPhases()
        {
            var fsoft = await _context.Enterprises.FirstAsync(e => e.Name == "FPT Software");
            var rikkeisoft = await _context.Enterprises.FirstAsync(e => e.Name == "Rikkeisoft");

            // Helper: create a phase via factory, then advance status via UpdateInfo
            async Task EnsurePhase(
                Guid enterpriseId, string name,
                DateOnly start, DateOnly end,
                int? maxStudents, string? description,
                InternshipPhaseStatus targetStatus)
            {
                if (await _context.InternshipPhases
                        .AnyAsync(p => p.EnterpriseId == enterpriseId && p.Name == name))
                    return;

                // Create always starts at Draft
                var phase = InternshipPhase.Create(enterpriseId, name, start, end, maxStudents, description);

                // Advance to target status via UpdateInfo
                if (targetStatus != InternshipPhaseStatus.Draft)
                    phase.UpdateInfo(name, start, end, maxStudents, description, targetStatus);

                _context.InternshipPhases.Add(phase);
            }

            // ── FPT Software phases ──────────────────────────────────────────────
            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Fall 2025",
                new DateOnly(2025, 9, 1), new DateOnly(2025, 12, 31),
                30, "Đợt thực tập Fall 2025 của FPT Software — đã kết thúc",
                InternshipPhaseStatus.Closed);

            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Spring 2026",
                new DateOnly(2026, 1, 15), new DateOnly(2026, 4, 30),
                50, "Đợt thực tập Spring 2026 của FPT Software — đang diễn ra",
                InternshipPhaseStatus.InProgress);

            await EnsurePhase(
                fsoft.EnterpriseId,
                "FPT Software Summer 2026",
                new DateOnly(2026, 5, 1), new DateOnly(2026, 8, 31),
                40, "Đợt thực tập Summer 2026 của FPT Software — đang tuyển",
                InternshipPhaseStatus.Open);

            // ── Rikkeisoft phases ────────────────────────────────────────────────
            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Fall 2025",
                new DateOnly(2025, 9, 1), new DateOnly(2025, 12, 31),
                20, "Đợt thực tập Fall 2025 của Rikkeisoft — đã kết thúc",
                InternshipPhaseStatus.Closed);

            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Spring 2026",
                new DateOnly(2026, 2, 1), new DateOnly(2026, 5, 31),
                20, "Đợt thực tập Spring 2026 của Rikkeisoft — đang diễn ra",
                InternshipPhaseStatus.InProgress);

            await EnsurePhase(
                rikkeisoft.EnterpriseId,
                "Rikkeisoft Summer 2026",
                new DateOnly(2026, 6, 1), new DateOnly(2026, 9, 30),
                15, "Đợt thực tập Summer 2026 của Rikkeisoft — bản nháp",
                InternshipPhaseStatus.Draft);

            await _context.SaveChangesAsync();
        }

        private async Task SeedInternshipGroups()
        {
            var fsoft = await _context.Enterprises.FirstAsync(e => e.Name == "FPT Software");
            var rikkeisoft = await _context.Enterprises.FirstAsync(e => e.Name == "Rikkeisoft");
            var mentorFpt = await _context.EnterpriseUsers.Include(eu => eu.User).FirstAsync(eu => eu.User.Email == "mentor@fptsoftware.com");
            var mentorRikkeis = await _context.EnterpriseUsers.Include(eu => eu.User).FirstAsync(eu => eu.User.Email == "mentor@rikkeisoft.com");

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

            // FPT Software OJT Team → Spring 2026 InProgress phase
            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team");
            if (group3 == null)
            {
                group3 = InternshipGroup.Create(phaseInProgressFpt.PhaseId, "FPT Software OJT Team", "Next-gen platform development", fsoft.EnterpriseId, mentorFpt.EnterpriseUserId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow.AddMonths(3));
                group3.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(group3);
            }

            // Rikkeisoft CRM Legacy → Rikkeisoft Fall 2025 closed phase (BUG-10 FIX: was using FPT's phase)
            var group5 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
            if (group5 == null)
            {
                group5 = InternshipGroup.Create(phaseClosedRikkei.PhaseId, "Rikkeisoft CRM Legacy", "Maintenance of legacy CRM", rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(-2));
                group5.UpdateStatus(GroupStatus.Finished);
                _context.InternshipGroups.Add(group5);
            }

            // Rikkeisoft Spring 2026 Team → Rikkeisoft InProgress phase
            var rikkeiActiveGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");
            if (rikkeiActiveGroup == null)
            {
                rikkeiActiveGroup = InternshipGroup.Create(phaseInProgressRikkei.PhaseId, "Rikkeisoft Spring 2026 Team", "Backend modernization and internal platform work", rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId, DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddMonths(2));
                rikkeiActiveGroup.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(rikkeiActiveGroup);
            }

            // FPT Software CT OJT Team → FPT Open (Summer) phase
            InternshipGroup? fptCtGroup = null;
            fptCtGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software CT OJT Team");
            if (fptCtGroup == null)
            {
                fptCtGroup = InternshipGroup.Create(phaseOpenFpt.PhaseId, "FPT Software CT OJT Team", "Cross-campus internship squad for FPTU Can Tho", fsoft.EnterpriseId, mentorFpt.EnterpriseUserId, DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddMonths(2));
                fptCtGroup.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(fptCtGroup);
            }

            await _context.SaveChangesAsync();

            // Link Students to Group 3 (Team for Student 3)
            if (!await _context.InternshipStudents.AnyAsync(x => x.InternshipId == group3.InternshipId && x.StudentId == s3.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group3.InternshipId, StudentId = s3.StudentId, Role = InternshipRole.Leader, Status = InternshipStatus.InProgress, JoinedAt = DateTime.UtcNow.AddMonths(-1) });
            }
            if (!await _context.InternshipStudents.AnyAsync(x => x.InternshipId == group3.InternshipId && x.StudentId == s1.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group3.InternshipId, StudentId = s1.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.InProgress, JoinedAt = DateTime.UtcNow.AddMonths(-1) });
            }
            if (!await _context.InternshipStudents.AnyAsync(x => x.InternshipId == group3.InternshipId && x.StudentId == s2.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group3.InternshipId, StudentId = s2.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.InProgress, JoinedAt = DateTime.UtcNow.AddMonths(-1) });
            }
            if (!await _context.InternshipStudents.AnyAsync(x => x.InternshipId == group3.InternshipId && x.StudentId == s4.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group3.InternshipId, StudentId = s4.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.InProgress, JoinedAt = DateTime.UtcNow.AddMonths(-1) });
            }

            if (!await _context.InternshipStudents.AnyAsync(x => x.InternshipId == group5.InternshipId && x.StudentId == s5.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group5.InternshipId, StudentId = s5.StudentId, Role = InternshipRole.Leader, Status = InternshipStatus.Completed, JoinedAt = DateTime.UtcNow.AddMonths(-6) });
            }

            if (!await _context.InternshipStudents.AnyAsync(x => x.InternshipId == rikkeiActiveGroup.InternshipId && x.StudentId == s2.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = rikkeiActiveGroup.InternshipId, StudentId = s2.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.InProgress, JoinedAt = DateTime.UtcNow.AddDays(-15) });
            }

            if (fptCtGroup != null && !await _context.InternshipStudents.AnyAsync(x => x.InternshipId == fptCtGroup.InternshipId && x.StudentId == s3.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = fptCtGroup.InternshipId, StudentId = s3.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.InProgress, JoinedAt = DateTime.UtcNow.AddDays(-12) });
            }

            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == null && a.StudentId == s3.StudentId))
            {
                var spring2026 = await _context.Terms.FirstAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");
                if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == spring2026.TermId && a.StudentId == s3.StudentId))
                    _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s3.StudentId, Status = InternshipApplicationStatus.Approved, AppliedAt = DateTime.UtcNow.AddDays(-40) });
            }

            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == rikkeisoft.EnterpriseId && a.StudentId == s2.StudentId))
            {
                var spring2026 = await _context.Terms.FirstAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s2.StudentId, Status = InternshipApplicationStatus.Approved, AppliedAt = DateTime.UtcNow.AddDays(-10) });
            }

            var spring2026Ct = await _context.Terms
                .Include(t => t.University)
                .FirstOrDefaultAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU-CT");
            if (spring2026Ct != null && fptCtGroup != null && !await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == spring2026Ct.TermId && a.StudentId == s4.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026Ct.TermId, StudentId = s4.StudentId, Status = InternshipApplicationStatus.Approved, AppliedAt = DateTime.UtcNow.AddDays(-8) });
            }

            // [NEW] Seed Pending and Rejected Applications
            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == rikkeisoft.EnterpriseId && a.StudentId == s4.StudentId))
            {
                var spring2026 = await _context.Terms.FirstAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s4.StudentId, Status = InternshipApplicationStatus.Pending, AppliedAt = DateTime.UtcNow.AddDays(-2) });
            }

            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.StudentId == s2.StudentId))
            {
                var fall2025 = await _context.Terms.FirstAsync(t => t.Name == "Fall 2025" && t.University.Code == "FPTU");
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = fall2025.TermId, StudentId = s2.StudentId, Status = InternshipApplicationStatus.Rejected, RejectReason = "Not a good fit for this semester", AppliedAt = DateTime.UtcNow.AddDays(-100) });
            }

            // [NEW] Seed Archived Group
            var archivedGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Archived Project");
            if (archivedGroup == null)
            {
                archivedGroup = InternshipGroup.Create(phaseClosedFpt.PhaseId, "FPT Archived Project", "Old project idea", fsoft.EnterpriseId, mentorFpt.EnterpriseUserId, DateTime.UtcNow.AddMonths(-12), DateTime.UtcNow.AddMonths(-10));
                archivedGroup.UpdateStatus(GroupStatus.Archived);
                _context.InternshipGroups.Add(archivedGroup);
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedProjectsAndWorkItems()
        {
            if (await _context.Projects.AnyAsync()) return;

            var group3 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "FPT Software OJT Team");
            var group5 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
            var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");

            // Project 3
            var proj3 = Project.Create(group3.InternshipId, "IOC v2.0 Platform", "Centralized internship management system");
            proj3.Update(null, null, null, DateTime.UtcNow.AddMonths(-1).AddDays(5), null, ProjectStatus.InProgress);
            _context.Projects.Add(proj3);

            // Project 5
            var proj5 = Project.Create(group5.InternshipId, "Legacy CRM Maintenance", "Fixing bugs and optimizing older modules");
            proj5.Update(null, null, null, DateTime.UtcNow.AddMonths(-6).AddDays(5), DateTime.UtcNow.AddMonths(-2).AddDays(-5), ProjectStatus.Done); // Fixed: Completed -> Done
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
            var projPending = Project.Create(group3.InternshipId, "FPT Future System", "Next phase architecture");
            projPending.Update(null, null, null, DateTime.UtcNow.AddDays(10), DateTime.UtcNow.AddDays(30), ProjectStatus.Planning);
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

                // [NEW] Add a sub-task for demonstration
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

        private async Task SeedLogbooks()
        {
            var proj3 = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "IOC v2.0 Platform");
            var proj5 = await _context.Projects.FirstOrDefaultAsync(p => p.ProjectName == "Legacy CRM Maintenance");
            var s1 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student1@fptu.edu.vn");
            var s2 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student5@fptu.edu.vn");

            if (proj3 == null || proj5 == null || s3 == null || s5 == null) return;

            if (!await _context.Logbooks.AnyAsync())
            {
                _context.Logbooks.AddRange(
                    Logbook.Create(proj3.InternshipId, s3.StudentId, "Integrated basic project structure.", null, "Focus on Auth module.", DateTime.UtcNow.AddDays(-7)),
                    Logbook.Create(proj3.InternshipId, s3.StudentId, "Started JWT implementation.", "Encountered some middleware issues.", "Resolve middleware and test login.", DateTime.UtcNow.AddDays(-1))
                );

                if (s1 != null) _context.Logbooks.Add(Logbook.Create(proj3.InternshipId, s1.StudentId, "Initial requirement analysis and documentation.", null, "Finalize SRS.", DateTime.UtcNow.AddDays(-6)));
                if (s2 != null) _context.Logbooks.Add(Logbook.Create(proj3.InternshipId, s2.StudentId, "UI/UX wireframing for main dashboard.", "Feedback from PO required.", "Update Figma design.", DateTime.UtcNow.AddDays(-5)));

                for (int i = 1; i <= 4; i++)
                {
                    _context.Logbooks.Add(Logbook.Create(proj5.InternshipId, s5.StudentId, $"Work report {i}", null, "Continue next task", DateTime.UtcNow.AddMonths(-6 + i)));
                }
                
                await _context.SaveChangesAsync();
            }

            // [NEW] Link WorkItems to Logbooks if not already linked (Using direct SQL for shadow junction table)
            bool alreadyLinked = await _context.Logbooks.AnyAsync(l => l.WorkItems.Any());
            if (!alreadyLinked)
            {
                var workItemsProj3 = await _context.WorkItems.Where(w => w.ProjectId == proj3.ProjectId).ToListAsync();
                var logbooksProj3 = await _context.Logbooks.Where(l => l.InternshipId == proj3.InternshipId).ToListAsync();
                
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

            var group3 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "FPT Software OJT Team");
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
            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team");
            var s3 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var mentorFpt = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mentor@fptsoftware.com");

            if (phaseFptSpring == null || group3 == null || s3 == null || mentorFpt == null) return;

            // Seed Evaluation Cycle
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

            // Seed Evaluation for student3
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
                        StudentId = null, // Group feedback
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
            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team");
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
                var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team");
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
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
            await SeedInternshipGroups();
            await SeedProjectsAndWorkItems();
            await SeedLogbooks();
            await SeedStakeholdersAndIssues();
            await SeedEvaluations();
            await SeedNotifications();

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

            // 3. 10 sinh viên — tất cả đã được place vào doanh nghiệp nhưng chưa có nhóm
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
                    // StudentIds cũ có 5 phần tử, sinh viên 6-10 dùng Guid mới
                    var userId = i < SeedIds.StudentIds.Count ? SeedIds.StudentIds[i] : Guid.NewGuid();
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

        private async Task SeedInternshipGroups()
        {
            var spring2026 = await _context.Terms.FirstAsync(t => t.Name == "Spring 2026");
            var fall2025 = await _context.Terms.FirstAsync(t => t.Name == "Fall 2025");
            var spring2026Ct = await _context.Terms
                .Include(t => t.University)
                .FirstOrDefaultAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU-CT");
            var fsoft = await _context.Enterprises.FirstAsync(e => e.Name == "FPT Software");
            var rikkeisoft = await _context.Enterprises.FirstAsync(e => e.Name == "Rikkeisoft");
            var mentorFpt = await _context.EnterpriseUsers.Include(eu => eu.User).FirstAsync(eu => eu.User.Email == "mentor@fptsoftware.com");
            var mentorRikkeis = await _context.EnterpriseUsers.Include(eu => eu.User).FirstAsync(eu => eu.User.Email == "mentor@rikkeisoft.com");

            // ── Tạo các nhóm thực tế (FPT Software - Spring 2026) ─────────────────
            var group1 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
            if (group1 == null)
            {
                group1 = InternshipGroup.Create(spring2026.TermId, "FPT Software OJT Team Alpha",
                    "Nhóm thực tập phát triển nền tảng thế hệ mới",
                    fsoft.EnterpriseId, mentorFpt.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddMonths(3));
                group1.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(group1);
            }

            var group2 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team Beta");
            if (group2 == null)
            {
                group2 = InternshipGroup.Create(spring2026.TermId, "FPT Software OJT Team Beta",
                    "Nhóm thực tập phát triển hệ thống backend",
                    fsoft.EnterpriseId, mentorFpt.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-28), DateTime.UtcNow.AddMonths(3));
                group2.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(group2);
            }

            // ── Tạo các nhóm thực tế (Rikkeisoft - Spring 2026) ──────────────────
            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team A");
            if (group3 == null)
            {
                group3 = InternshipGroup.Create(spring2026.TermId, "Rikkeisoft Spring 2026 Team A",
                    "Nhóm hiện đại hoá backend và nền tảng nội bộ",
                    rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddMonths(2));
                group3.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(group3);
            }

            var group4 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team B");
            if (group4 == null)
            {
                group4 = InternshipGroup.Create(spring2026.TermId, "Rikkeisoft Spring 2026 Team B",
                    "Nhóm phát triển giao diện và tích hợp hệ thống",
                    rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-18), DateTime.UtcNow.AddMonths(2));
                group4.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(group4);
            }

            // ── Nhóm đã Finished (Fall 2025) ─────────────────────────────────────
            var groupOld = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
            if (groupOld == null)
            {
                groupOld = InternshipGroup.Create(fall2025.TermId, "Rikkeisoft CRM Legacy",
                    "Bảo trì hệ thống CRM cũ",
                    rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId,
                    DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(-2));
                groupOld.UpdateStatus(GroupStatus.Finished);
                _context.InternshipGroups.Add(groupOld);
            }

            // ── Nhóm FPTU-CT (Spring 2026) ────────────────────────────────────────
            if (spring2026Ct != null)
            {
                var groupCt = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software CT OJT Team");
                if (groupCt == null)
                {
                    groupCt = InternshipGroup.Create(spring2026Ct.TermId, "FPT Software CT OJT Team",
                        "Nhóm thực tập liên campus FPTU Cần Thơ",
                        fsoft.EnterpriseId, mentorFpt.EnterpriseUserId,
                        DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddMonths(2));
                    groupCt.UpdateStatus(GroupStatus.Active);
                    _context.InternshipGroups.Add(groupCt);
                }
            }

            await _context.SaveChangesAsync();

            // ── Nhóm test DELETE (không có sinh viên) ────────────────────────────
            var deleteTestGroup1 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "[TEST] FPT Active - Xóa được");
            if (deleteTestGroup1 == null)
            {
                deleteTestGroup1 = InternshipGroup.Create(spring2026.TermId, "[TEST] FPT Active - Xóa được",
                    "Nhóm Active, không có logbook/vi phạm → có thể xóa",
                    fsoft.EnterpriseId, mentorFpt.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddMonths(2));
                deleteTestGroup1.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(deleteTestGroup1);
            }

            var deleteTestGroup2 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "[TEST] Rikkei Finished - Không xóa được");
            if (deleteTestGroup2 == null)
            {
                deleteTestGroup2 = InternshipGroup.Create(spring2026.TermId, "[TEST] Rikkei Finished - Không xóa được",
                    "Nhóm đã Finished → bị chặn khi xóa do không phải Active",
                    rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-5));
                deleteTestGroup2.UpdateStatus(GroupStatus.Finished);
                _context.InternshipGroups.Add(deleteTestGroup2);
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

            // FPT Software: student1, student2, student3, student6, student7, student8
            var fstudents = new[] { 0, 1, 2, 5, 6, 7 };
            // Rikkeisoft:   student4, student5, student9, student10
            var rstudents = new[] { 3, 4, 8, 9 };

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
                        Status = InternshipApplicationStatus.Approved,
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
                        Status = InternshipApplicationStatus.Approved,
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

            await _context.SaveChangesAsync();
        }

        private async Task SeedLogbooks()
        {
            if (await _context.Logbooks.AnyAsync()) return;

            var proj3 = await _context.Projects.FirstAsync(p => p.ProjectName == "IOC v2.0 Platform");
            var proj5 = await _context.Projects.FirstAsync(p => p.ProjectName == "Legacy CRM Maintenance");
            var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");

            _context.Logbooks.AddRange(
                Logbook.Create(proj3.InternshipId, s3.StudentId, "Integrated basic project structure.", null, "Focus on Auth module.", DateTime.UtcNow.AddDays(-7)),
                Logbook.Create(proj3.InternshipId, s3.StudentId, "Started JWT implementation.", "Encountered some middleware issues.", "Resolve middleware and test login.", DateTime.UtcNow.AddDays(-1))
            );

            for (int i = 1; i <= 4; i++)
            {
                _context.Logbooks.Add(Logbook.Create(proj5.InternshipId, s5.StudentId, $"Work report {i}", null, "Continue next task", DateTime.UtcNow.AddMonths(-6 + i)));
            }


            await _context.SaveChangesAsync();
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
            var now = DateTime.UtcNow;

            static DateTime ToUtc(DateOnly date, int hour, int minute = 0)
                => DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, minute)), DateTimeKind.Utc);

            EvaluationCycleStatus ResolveStatus(DateTime startDate, DateTime endDate)
            {
                if (now < startDate) return EvaluationCycleStatus.Pending;
                if (now <= endDate) return EvaluationCycleStatus.Grading;
                return EvaluationCycleStatus.Completed;
            }

            async Task EnsureCycleAsync(Guid termId, string cycleName, DateTime startDate, DateTime endDate)
            {
                var exists = await _context.EvaluationCycles
                    .AnyAsync(c => c.TermId == termId && c.Name == cycleName);

                if (!exists)
                {
                    _context.EvaluationCycles.Add(new EvaluationCycle
                    {
                        CycleId = Guid.NewGuid(),
                        TermId = termId,
                        Name = cycleName,
                        StartDate = startDate,
                        EndDate = endDate,
                        Status = ResolveStatus(startDate, endDate),
                        CreatedAt = now
                    });
                }
            }

            var fall2025 = await _context.Terms
                .Include(t => t.University)
                .FirstAsync(t => t.Name == "Fall 2025" && t.University.Code == "FPTU");

            var spring2026 = await _context.Terms
                .Include(t => t.University)
                .FirstAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU");

            var spring2026Ct = await _context.Terms
                .Include(t => t.University)
                .FirstAsync(t => t.Name == "Spring 2026" && t.University.Code == "FPTU-CT");

            // Active-term deadline data for enterprise timeline APIs.
            var midtermSpring2026 = await _context.EvaluationCycles.FirstOrDefaultAsync(c => c.TermId == spring2026.TermId && c.Name == "Midterm Evaluation");
            if (midtermSpring2026 == null)
            {
                midtermSpring2026 = new EvaluationCycle
                {
                    CycleId = Guid.NewGuid(),
                    TermId = spring2026.TermId,
                    Name = "Midterm Evaluation",
                    StartDate = ToUtc(spring2026.StartDate.AddDays(30), 8),
                    EndDate = ToUtc(spring2026.StartDate.AddDays(75), 23, 59),
                    Status = EvaluationCycleStatus.Grading,
                    CreatedAt = now
                };
                _context.EvaluationCycles.Add(midtermSpring2026);
            }

            var springFinalCycle = await _context.EvaluationCycles.FirstOrDefaultAsync(c => c.TermId == spring2026.TermId && c.Name == "Final Evaluation");
            if (springFinalCycle == null)
            {
                springFinalCycle = new EvaluationCycle
                {
                    CycleId = Guid.NewGuid(),
                    TermId = spring2026.TermId,
                    Name = "Final Evaluation",
                    StartDate = ToUtc(spring2026.StartDate.AddDays(90), 8),
                    EndDate = ToUtc(spring2026.EndDate.AddDays(-2), 23, 59),
                    Status = EvaluationCycleStatus.Pending,
                    CreatedAt = now
                };
                _context.EvaluationCycles.Add(springFinalCycle);
            }

            await EnsureCycleAsync(
                spring2026Ct.TermId,
                "Midterm Evaluation",
                ToUtc(spring2026Ct.StartDate.AddDays(20), 8),
                ToUtc(spring2026Ct.StartDate.AddDays(65), 23, 59));

            await EnsureCycleAsync(
                spring2026Ct.TermId,
                "Final Evaluation",
                ToUtc(spring2026Ct.StartDate.AddDays(80), 8),
                ToUtc(spring2026Ct.EndDate.AddDays(-2), 23, 59));

            var fallCycle = await _context.EvaluationCycles
                .FirstOrDefaultAsync(c => c.TermId == fall2025.TermId && c.Name == "Final Evaluation");

            if (fallCycle == null)
            {
                fallCycle = new EvaluationCycle
                {
                    CycleId = Guid.NewGuid(),
                    TermId = fall2025.TermId,
                    Name = "Final Evaluation",
                    StartDate = ToUtc(fall2025.EndDate.AddDays(-45), 8),
                    EndDate = ToUtc(fall2025.EndDate.AddDays(-15), 23, 59),
                    Status = EvaluationCycleStatus.Completed,
                    CreatedAt = now
                };
                _context.EvaluationCycles.Add(fallCycle);
            }

            var criteria = await _context.EvaluationCriteria
                .FirstOrDefaultAsync(c => c.CycleId == fallCycle.CycleId && c.Name == "Technical Skills");

            if (criteria == null)
            {
                criteria = new EvaluationCriteria
                {
                    CriteriaId = Guid.NewGuid(),
                    CycleId = fallCycle.CycleId,
                    Name = "Technical Skills",
                    Description = "Programming and problem solving",
                    MaxScore = 100m,
                    Weight = 50m
                };
                _context.EvaluationCriteria.Add(criteria);
            }

            var hasFallEvaluation = await _context.Evaluations
                .AnyAsync(e => e.CycleId == fallCycle.CycleId);

            if (!hasFallEvaluation)
            {
                var group5 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
                var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");
                var mentorUser = await _context.Users.FirstAsync(u => u.Email == "mentor@rikkeisoft.com");

                var evaluation = new Evaluation
                {
                    EvaluationId = Guid.NewGuid(),
                    CycleId = fallCycle.CycleId,
                    InternshipId = group5.InternshipId,
                    StudentId = s5.StudentId,
                    EvaluatorId = mentorUser.UserId,
                    Status = EvaluationStatus.Published,
                    TotalScore = 95m,
                    Note = "Excellent performance throughout the term."
                };
                _context.Evaluations.Add(evaluation);

                _context.EvaluationDetails.Add(new EvaluationDetail
                {
                    DetailId = Guid.NewGuid(),
                    EvaluationId = evaluation.EvaluationId,
                    CriteriaId = criteria.CriteriaId,
                    Score = 95m,
                    Comment = "Strong understanding of legacy code."
                });
            }

            var hasSpringEvaluation = await _context.Evaluations
                .AnyAsync(e => e.CycleId == midtermSpring2026.CycleId);

            if (!hasSpringEvaluation)
            {
                var group3 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "FPT Software OJT Team Alpha");
                var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
                var mentorUser = await _context.Users.FirstAsync(u => u.Email == "mentor@fptsoftware.com");

                // Get or create technical skill criteria for Midterm
                var springMidCriteria = await _context.EvaluationCriteria
                    .FirstOrDefaultAsync(c => c.CycleId == midtermSpring2026.CycleId && c.Name == "Technical Skills");

                if (springMidCriteria == null)
                {
                    springMidCriteria = new EvaluationCriteria
                    {
                        CriteriaId = Guid.NewGuid(),
                        CycleId = midtermSpring2026.CycleId,
                        Name = "Technical Skills",
                        Description = "Development ability",
                        MaxScore = 100m,
                        Weight = 50m
                    };
                    _context.EvaluationCriteria.Add(springMidCriteria);
                }

                var evaluationS3 = new Evaluation
                {
                    EvaluationId = Guid.NewGuid(),
                    CycleId = midtermSpring2026.CycleId,
                    InternshipId = group3.InternshipId,
                    StudentId = s3.StudentId,
                    EvaluatorId = mentorUser.UserId,
                    Status = EvaluationStatus.Published,
                    TotalScore = 88m,
                    Note = "Great progress in the first half."
                };
                _context.Evaluations.Add(evaluationS3);

                _context.EvaluationDetails.Add(new EvaluationDetail
                {
                    DetailId = Guid.NewGuid(),
                    EvaluationId = evaluationS3.EvaluationId,
                    CriteriaId = springMidCriteria.CriteriaId,
                    Score = 88m,
                    Comment = "Good coding practices."
                });
            }

            await _context.SaveChangesAsync();
            await _context.SaveChangesAsync();
        }

        private async Task SeedNotifications()
        {
            if (await _context.Set<Notification>().AnyAsync()) return;

            var s3User = await _context.Users.FirstOrDefaultAsync(u => u.Email == "student3@fptu.edu.vn");
            var devUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "trunguyen.104@gmail.com");

            var notifications = new List<Notification>();

            if (s3User != null)
            {
                notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = s3User.UserId, Title = "Hồ sơ thực tập được duyệt", Content = "Hồ sơ thực tập của bạn tại FPT Software đã được duyệt.", Type = NotificationType.ApplicationAccepted, ReferenceType = "InternshipApplication", IsRead = false });
                notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = s3User.UserId, Title = "Nhắc nhở nộp báo cáo", Content = "Sắp đến hạn nộp báo cáo định kỳ. Vui lòng cập nhật Logbook.", Type = NotificationType.LogbookFeedback, IsRead = true, ReadAt = DateTime.UtcNow.AddDays(-1) });
            }

            if (devUser != null)
            {
                for (int i = 1; i <= 10; i++)
                {
                    notifications.Add(new Notification { NotificationId = Guid.NewGuid(), UserId = devUser.UserId, Title = $"Thông báo Demo số {i}", Content = $"Đây là nội dung chi tiết cho thông báo demo số {i}. Vui lòng kiểm tra chức năng hệ thống.", Type = NotificationType.General, IsRead = (i % 3 == 0), ReadAt = (i % 3 == 0) ? DateTime.UtcNow.AddHours(-i) : null });
                }
            }

            if (notifications.Any())
            {
                _context.Set<Notification>().AddRange(notifications);
                await _context.SaveChangesAsync();
            }
        }
    }
}

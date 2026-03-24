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

            // 3. 5 Specific Students
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

            var s2 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s4 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student4@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");

            var group3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software OJT Team");
            if (group3 == null)
            {
                group3 = InternshipGroup.Create(spring2026.TermId, "FPT Software OJT Team", "Next-gen platform development", fsoft.EnterpriseId, mentorFpt.EnterpriseUserId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow.AddMonths(3));
                group3.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(group3);
            }

            var group5 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
            if (group5 == null)
            {
                group5 = InternshipGroup.Create(fall2025.TermId, "Rikkeisoft CRM Legacy", "Maintenance of legacy CRM", rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(-2));
                group5.UpdateStatus(GroupStatus.Finished); // Fixed: Closed -> Finished
                _context.InternshipGroups.Add(group5);
            }

            var rikkeiActiveGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "Rikkeisoft Spring 2026 Team");
            if (rikkeiActiveGroup == null)
            {
                rikkeiActiveGroup = InternshipGroup.Create(spring2026.TermId, "Rikkeisoft Spring 2026 Team", "Backend modernization and internal platform work", rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId, DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddMonths(2));
                rikkeiActiveGroup.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(rikkeiActiveGroup);
            }

            InternshipGroup? fptCtGroup = null;
            if (spring2026Ct != null)
            {
                fptCtGroup = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "FPT Software CT OJT Team");
                if (fptCtGroup == null)
                {
                    fptCtGroup = InternshipGroup.Create(spring2026Ct.TermId, "FPT Software CT OJT Team", "Cross-campus internship squad for FPTU Can Tho", fsoft.EnterpriseId, mentorFpt.EnterpriseUserId, DateTime.UtcNow.AddDays(-15), DateTime.UtcNow.AddMonths(2));
                    fptCtGroup.UpdateStatus(GroupStatus.Active);
                    _context.InternshipGroups.Add(fptCtGroup);
                }
            }

            await _context.SaveChangesAsync();

            // ── Thêm nhóm chuyên dùng để test DELETE AC-G09 ───────────────────────
            // [TEST-DELETE-1] Active + KHÔNG có data thực tế + còn SV → CÓ THỂ XÓA
            var deleteTestGroup1 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "[TEST] FPT Active - Xóa được");
            if (deleteTestGroup1 == null)
            {
                deleteTestGroup1 = InternshipGroup.Create(spring2026.TermId, "[TEST] FPT Active - Xóa được",
                    "Nhóm Active, có SV nhưng KHÔNG có logbook/vi phạm/workitem → có thể xóa",
                    fsoft.EnterpriseId, mentorFpt.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddMonths(2));
                deleteTestGroup1.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(deleteTestGroup1);
            }

            // [TEST-DELETE-2] Active + CÓ logbook → BỊ CHẶN (có data thực tế)
            var deleteTestGroup2 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "[TEST] FPT Active - Không xóa được (có data)");
            if (deleteTestGroup2 == null)
            {
                deleteTestGroup2 = InternshipGroup.Create(spring2026.TermId, "[TEST] FPT Active - Không xóa được (có data)",
                    "Nhóm Active, có logbook → bị chặn khi xóa",
                    fsoft.EnterpriseId, mentorFpt.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddMonths(2));
                deleteTestGroup2.UpdateStatus(GroupStatus.Active);
                _context.InternshipGroups.Add(deleteTestGroup2);
            }

            // [TEST-DELETE-3] Finished → BỊ CHẶN (không phải Active)
            var deleteTestGroup3 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "[TEST] Rikkei Finished - Không xóa được (Finished)");
            if (deleteTestGroup3 == null)
            {
                deleteTestGroup3 = InternshipGroup.Create(spring2026.TermId, "[TEST] Rikkei Finished - Không xóa được (Finished)",
                    "Nhóm đã Finished → bị chặn khi xóa do không phải Active",
                    rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId,
                    DateTime.UtcNow.AddDays(-60), DateTime.UtcNow.AddDays(-5));
                deleteTestGroup3.UpdateStatus(GroupStatus.Finished);
                _context.InternshipGroups.Add(deleteTestGroup3);
            }

            await _context.SaveChangesAsync();

            // Link Students to Groups
            if (!await _context.InternshipStudents.AnyAsync(x => x.InternshipId == group3.InternshipId && x.StudentId == s3.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group3.InternshipId, StudentId = s3.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.InProgress, JoinedAt = DateTime.UtcNow.AddMonths(-1) });
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

            // Thêm s4 vào TEST-DELETE-1 (nhóm Active, có SV nhưng không có data → xóa được)
            if (deleteTestGroup1 != null && !await _context.InternshipStudents.AnyAsync(x => x.InternshipId == deleteTestGroup1.InternshipId && x.StudentId == s4.StudentId))
            {
                _context.InternshipStudents.Add(new InternshipStudent { InternshipId = deleteTestGroup1.InternshipId, StudentId = s4.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.Registered, JoinedAt = DateTime.UtcNow.AddDays(-5) });
            }

            await _context.SaveChangesAsync();

            // ── Các đơn đã Approved (giữ nguyên) ─────────────────────────────────
            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == spring2026.TermId && a.StudentId == s3.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s3.StudentId, Status = InternshipApplicationStatus.Approved, AppliedAt = DateTime.UtcNow.AddDays(-40) });
            }

            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == rikkeisoft.EnterpriseId && a.TermId == spring2026.TermId && a.StudentId == s2.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s2.StudentId, Status = InternshipApplicationStatus.Approved, AppliedAt = DateTime.UtcNow.AddDays(-10) });
            }

            if (spring2026Ct != null && !await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == spring2026Ct.TermId && a.StudentId == s4.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026Ct.TermId, StudentId = s4.StudentId, Status = InternshipApplicationStatus.Approved, AppliedAt = DateTime.UtcNow.AddDays(-8) });
            }

            // ── Đơn Pending: FPT Software - Spring 2026 (FPTU) ──────────────────
            var s1 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student1@fptu.edu.vn");

            if (s1 != null && !await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == spring2026.TermId && a.StudentId == s1.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s1.StudentId, Status = InternshipApplicationStatus.Pending, AppliedAt = DateTime.UtcNow.AddDays(-5) });
            }

            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == spring2026.TermId && a.StudentId == s4.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s4.StudentId, Status = InternshipApplicationStatus.Pending, AppliedAt = DateTime.UtcNow.AddDays(-3) });
            }

            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == spring2026.TermId && a.StudentId == s5.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s5.StudentId, Status = InternshipApplicationStatus.Pending, AppliedAt = DateTime.UtcNow.AddDays(-1) });
            }

            // ── Đơn Pending: Rikkeisoft - Spring 2026 (FPTU) ────────────────────
            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == rikkeisoft.EnterpriseId && a.TermId == spring2026.TermId && a.StudentId == s4.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s4.StudentId, Status = InternshipApplicationStatus.Pending, AppliedAt = DateTime.UtcNow.AddDays(-7) });
            }

            if (!await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == rikkeisoft.EnterpriseId && a.TermId == spring2026.TermId && a.StudentId == s5.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s5.StudentId, Status = InternshipApplicationStatus.Pending, AppliedAt = DateTime.UtcNow.AddDays(-2) });
            }

            // ── Đơn Pending: FPT Software - Summer 2026 (Upcoming) ───────────────
            var summer2026 = await _context.Terms
                .Include(t => t.University)
                .FirstOrDefaultAsync(t => t.Name == "Summer 2026" && t.University.Code == "FPTU");

            if (summer2026 != null && s1 != null && !await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == summer2026.TermId && a.StudentId == s1.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = summer2026.TermId, StudentId = s1.StudentId, Status = InternshipApplicationStatus.Pending, AppliedAt = DateTime.UtcNow.AddDays(-2) });
            }

            if (summer2026 != null && !await _context.InternshipApplications.AnyAsync(a => a.EnterpriseId == fsoft.EnterpriseId && a.TermId == summer2026.TermId && a.StudentId == s2.StudentId))
            {
                _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = summer2026.TermId, StudentId = s2.StudentId, Status = InternshipApplicationStatus.Pending, AppliedAt = DateTime.UtcNow.AddDays(-1) });
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

            // Thêm logbook cho TEST-DELETE-2 → nhóm này có data thực tế, KHÔNG thể xóa
            var deleteTestGroup2 = await _context.InternshipGroups.FirstOrDefaultAsync(g => g.GroupName == "[TEST] FPT Active - Không xóa được (có data)");
            if (deleteTestGroup2 != null)
            {
                _context.Logbooks.Add(Logbook.Create(
                    deleteTestGroup2.InternshipId, s3.StudentId,
                    "Báo cáo test — nhóm này có data, không thể xóa.", null,
                    "Kiểm tra chức năng chặn xóa nhóm.", DateTime.UtcNow.AddDays(-3)));
            }

            await _context.SaveChangesAsync();
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
                var group3 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "FPT Software OJT Team");
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

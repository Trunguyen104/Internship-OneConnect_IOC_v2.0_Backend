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
            await SeedUniversities();
            await SeedEnterprises();
            await SeedJobs(); // added: seed test jobs for enterprises
            await SeedUsers();
            await SeedTerms();
            await SeedInternshipGroups();
            await SeedProjectsAndWorkItems();
            await SeedLogbooks();
            await SeedEvaluations();

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

            // New: add a sixth seeded student (student6) with a CV for job-apply testing
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
                    InternshipStatus = StudentStatus.APPLIED, // eligible to apply (not in-progress / completed)
                    Major = "Software Engineering",
                    ClassName = "SE1616"
                };

                // Attach CV so student6 can apply to jobs (use a deterministic reachable test URL)
                student6.UpdateCv("https://iocv2-test-resources.s3.amazonaws.com/resumes/student6_cv.pdf");

                _context.Students.Add(student6);
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedTerms()
        {
            if (!await _context.Terms.AnyAsync())
            {
                var fptu = await _context.Universities.FirstAsync(u => u.Code == "FPTU");
                
                var fall2025 = new Term { TermId = Guid.NewGuid(), UniversityId = fptu.UniversityId, Name = "Fall 2025", StartDate = new DateOnly(2025, 9, 1), EndDate = new DateOnly(2025, 12, 31), Status = TermStatus.Closed };
                var spring2026 = new Term { TermId = Guid.NewGuid(), UniversityId = fptu.UniversityId, Name = "Spring 2026", StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 4, 30), Status = TermStatus.Open };
                var summer2026 = new Term { TermId = Guid.NewGuid(), UniversityId = fptu.UniversityId, Name = "Summer 2026", StartDate = new DateOnly(2026, 5, 1), EndDate = new DateOnly(2026, 8, 31), Status = TermStatus.Open };

                _context.Terms.AddRange(fall2025, spring2026, summer2026);
                await _context.SaveChangesAsync();

                // Enrollment for students
                var s2 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student2@fptu.edu.vn");
                var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
                var s4 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student4@fptu.edu.vn");
                var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");

                _context.StudentTerms.Add(new StudentTerm { StudentTermId = Guid.NewGuid(), StudentId = s2.StudentId, TermId = spring2026.TermId, EnrollmentStatus = EnrollmentStatus.Active, PlacementStatus = PlacementStatus.Unplaced, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)) });
                _context.StudentTerms.Add(new StudentTerm { StudentTermId = Guid.NewGuid(), StudentId = s3.StudentId, TermId = spring2026.TermId, EnrollmentStatus = EnrollmentStatus.Active, PlacementStatus = PlacementStatus.Placed, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-60)) });
                _context.StudentTerms.Add(new StudentTerm { StudentTermId = Guid.NewGuid(), StudentId = s4.StudentId, TermId = summer2026.TermId, EnrollmentStatus = EnrollmentStatus.Active, PlacementStatus = PlacementStatus.Unplaced, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)) });
                _context.StudentTerms.Add(new StudentTerm { StudentTermId = Guid.NewGuid(), StudentId = s5.StudentId, TermId = fall2025.TermId, EnrollmentStatus = EnrollmentStatus.Active, PlacementStatus = PlacementStatus.Placed, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-200)) });

                // Ensure seeded student6 is enrolled so they can apply for jobs
                var maybeS6 = await _context.Students.Include(s => s.User).FirstOrDefaultAsync(s => s.User.Email == "student6@fptu.edu.vn");
                if (maybeS6 != null)
                {
                    _context.StudentTerms.Add(new StudentTerm
                    {
                        StudentTermId = Guid.NewGuid(),
                        StudentId = maybeS6.StudentId,
                        TermId = spring2026.TermId,
                        EnrollmentStatus = EnrollmentStatus.Active,
                        PlacementStatus = PlacementStatus.Unplaced,
                        EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10))
                    });
                }

                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedInternshipGroups()
        {
            if (await _context.InternshipGroups.AnyAsync()) return;

            var spring2026 = await _context.Terms.FirstAsync(t => t.Name == "Spring 2026");
            var fall2025 = await _context.Terms.FirstAsync(t => t.Name == "Fall 2025");
            var fsoft = await _context.Enterprises.FirstAsync(e => e.Name == "FPT Software");
            var rikkeisoft = await _context.Enterprises.FirstAsync(e => e.Name == "Rikkeisoft");
            var mentorFpt = await _context.EnterpriseUsers.Include(eu => eu.User).FirstAsync(eu => eu.User.Email == "mentor@fptsoftware.com");
            var mentorRikkeis = await _context.EnterpriseUsers.Include(eu => eu.User).FirstAsync(eu => eu.User.Email == "mentor@rikkeisoft.com");

            var s2 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student2@fptu.edu.vn");
            var s3 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student3@fptu.edu.vn");
            var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");

            // Group for Student 3 (Active)
            var group3 = InternshipGroup.Create(spring2026.TermId, "FPT Software OJT Team", "Next-gen platform development", fsoft.EnterpriseId, mentorFpt.EnterpriseUserId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow.AddMonths(3));
            group3.UpdateStatus(GroupStatus.Active);
            _context.InternshipGroups.Add(group3);

            // Group for Student 5 (Completed)
            var group5 = InternshipGroup.Create(fall2025.TermId, "Rikkeisoft CRM Legacy", "Maintenance of legacy CRM", rikkeisoft.EnterpriseId, mentorRikkeis.EnterpriseUserId, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(-2));
            group5.UpdateStatus(GroupStatus.Finished); // Fixed: Closed -> Finished
            _context.InternshipGroups.Add(group5);

            await _context.SaveChangesAsync();

            // Link Students to Groups
            _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group3.InternshipId, StudentId = s3.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.InProgress, JoinedAt = DateTime.UtcNow.AddMonths(-1) });
            _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group5.InternshipId, StudentId = s5.StudentId, Role = InternshipRole.Leader, Status = InternshipStatus.Completed, JoinedAt = DateTime.UtcNow.AddMonths(-6) });

            // Seed some applications
            // Ensure JobId is set for each seeded application to satisfy FK constraint (job_id is required)
            var fptJob = await _context.Jobs.FirstAsync(j => j.EnterpriseId == SeedIds.FptSoftwareId);
            var rikkeiJob = await _context.Jobs.FirstAsync(j => j.EnterpriseId == SeedIds.RikkeisoftId);

            _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = fsoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s3.StudentId, JobId = fptJob.JobId, Status = InternshipApplicationStatus.Placed, AppliedAt = DateTime.UtcNow.AddDays(-40) });
            _context.InternshipApplications.Add(new InternshipApplication { ApplicationId = Guid.NewGuid(), EnterpriseId = rikkeisoft.EnterpriseId, TermId = spring2026.TermId, StudentId = s2.StudentId, JobId = rikkeiJob.JobId, Status = InternshipApplicationStatus.PendingAssignment, AppliedAt = DateTime.UtcNow.AddDays(-10) });

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
            
            await _context.SaveChangesAsync();
        }

        private async Task SeedEvaluations()
        {
            if (await _context.Evaluations.AnyAsync()) return;

            var fall2025 = await _context.Terms.FirstAsync(t => t.Name == "Fall 2025");
            var group5 = await _context.InternshipGroups.FirstAsync(g => g.GroupName == "Rikkeisoft CRM Legacy");
            var s5 = await _context.Students.Include(s => s.User).FirstAsync(s => s.User.Email == "student5@fptu.edu.vn");
            var mentorUser = await _context.Users.FirstAsync(u => u.Email == "mentor@rikkeisoft.com");

            var cycle = new EvaluationCycle { CycleId = Guid.NewGuid(), TermId = fall2025.TermId, Name = "Final Evaluation", StartDate = DateTime.UtcNow.AddMonths(-3), EndDate = DateTime.UtcNow.AddMonths(-2), Status = EvaluationCycleStatus.Completed };
            _context.EvaluationCycles.Add(cycle);
            await _context.SaveChangesAsync();

            var criteria = new EvaluationCriteria { CriteriaId = Guid.NewGuid(), CycleId = cycle.CycleId, Name = "Technical Skills", Description = "Programming and problem solving", MaxScore = 100m, Weight = 50m };
            _context.EvaluationCriteria.Add(criteria);
            await _context.SaveChangesAsync();

            var evaluation = new Evaluation { EvaluationId = Guid.NewGuid(), CycleId = cycle.CycleId, InternshipId = group5.InternshipId, StudentId = s5.StudentId, EvaluatorId = mentorUser.UserId, Status = EvaluationStatus.Published, TotalScore = 95m, Note = "Excellent performance throughout the term." }; // Fixed: Completed -> Published
            _context.Evaluations.Add(evaluation);
            
            _context.EvaluationDetails.Add(new EvaluationDetail { DetailId = Guid.NewGuid(), EvaluationId = evaluation.EvaluationId, CriteriaId = criteria.CriteriaId, Score = 95m, Comment = "Strong understanding of legacy code." });

            await _context.SaveChangesAsync();
        }
    }
}

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
            await SeedProjects();
            await SeedLogbooks();

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
            var existingEmails = await _context.Users.Select(u => u.Email).ToHashSetAsync();
            CancellationToken cancellationToken = default;

            // 1. Super Admin
            if (!await _context.Users.AnyAsync(u => u.Role == UserRole.SuperAdmin))
            {
                var userId = Guid.NewGuid();
                var userCode = await _userService.GenerateUserCodeAsync(UserRole.SuperAdmin, cancellationToken);
                var superAdmin = new User(
                    userId,
                    userCode,
                    "admin@iocv2.com",
                    "Super Administrator",
                    UserRole.SuperAdmin,
                    passHash
                );
                // Status is Active by default in constructor, but we can set it explicitly if needed
                superAdmin.SetStatus(UserStatus.Active);
                superAdmin.UpdateProfile("Super Administrator", null, null, UserGender.Other, null);

                _context.Users.Add(superAdmin);
            }
            // Enterprise Admins
            foreach (var ent in enterpriseList)
            {
                var email = $"admin@{ent.Name.Replace(" ", "").ToLower()}.com";
                if (!existingEmails.Contains(email))
                {
                    var userId = Guid.NewGuid();
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.EnterpriseAdmin, cancellationToken);

                    var user = new User(
                        userId,
                        userCode,
                        email,
                        $"Enterprise Admin of {ent.Name}",
                        UserRole.EnterpriseAdmin,
                        passHash
                    );

                    user.SetStatus(UserStatus.Active);

                    _context.Users.Add(user);

                    _context.EnterpriseUsers.Add(new EnterpriseUser
                    {
                        EnterpriseUserId = Guid.NewGuid(),
                        UserId = user.UserId,
                        EnterpriseId = ent.EnterpriseId,
                        Position = "Enterprise Administrator"
                    });
                }
            }

            // HRs
            foreach (var ent in enterpriseList)
            {
                var email = $"hr@{ent.Name.Replace(" ", "").ToLower()}.com";
                if (!existingEmails.Contains(email))
                {
                    var userId = Guid.NewGuid();
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.HR, cancellationToken);

                    var user = new User(
                        userId,
                        userCode,
                        email,
                        $"HR of {ent.Name}",
                        UserRole.HR,
                        passHash
                    );

                    user.SetStatus(UserStatus.Active);

                    _context.Users.Add(user);

                    _context.EnterpriseUsers.Add(new EnterpriseUser
                    {
                        EnterpriseUserId = Guid.NewGuid(),
                        UserId = user.UserId,
                        EnterpriseId = ent.EnterpriseId,
                        Position = "HR Manager"
                    });
                }
            }

            // 6. Students
            foreach (var uni in universityList)
            {
                int count = uni.Code == "FPTU" ? 3 : 2;
                for (int i = 1; i <= count; i++)
                {
                    var email = $"student{i}@{uni.Code.ToLower()}.edu.vn";
                    if (!existingEmails.Contains(email))
                    {
                        var userId = Guid.NewGuid();
                        var userCode = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                        var user = new User(
                            userId,
                            userCode,
                            email,
                            $"Student {i} of {uni.Code}",
                            UserRole.Student,
                            passHash
                        );
                        user.SetStatus(UserStatus.Active);
                        _context.Users.Add(user);
                        _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user.UserId, UniversityId = uni.UniversityId });
                        _context.Students.Add(new Student
                        {
                            StudentId = Guid.NewGuid(),
                            UserId = user.UserId,
                            InternshipStatus = StudentStatus.NO_INTERNSHIP,
                            Major = uni.Code == "FPTU" ? "Computer Science" : "Business Administration",
                            ClassName = $"{uni.Code}_K{65 + i}"
                        });
                    }
                }
            }
            await _context.SaveChangesAsync();

            // 7. Mentors (Enterprise Users)
            foreach (var ent in enterpriseList)
            {
                var email = $"mentor@{ent.Name.Replace(" ", "").ToLower()}.com";
                if (!existingEmails.Contains(email))
                {
                    var userId = Guid.NewGuid();
                    var userCode = await _userService.GenerateUserCodeAsync(UserRole.Mentor, cancellationToken);
                    var user = new User(
                        userId,
                        userCode,
                        email,
                        $"Mentor at {ent.Name}",
                        UserRole.Mentor,
                        passHash
                    );
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    _context.EnterpriseUsers.Add(new EnterpriseUser
                    {
                        EnterpriseUserId = Guid.NewGuid(),
                        UserId = user.UserId,
                        EnterpriseId = ent.EnterpriseId,
                        Position = "Senior Engineer"
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task SeedTerms()
        {
            if (!await _context.Terms.AnyAsync())
            {
                var fptu = await _context.Universities.FirstAsync(u => u.Code == "FPTU");
                var term = new Term
                {
                    TermId = Guid.NewGuid(),
                    UniversityId = fptu.UniversityId,
                    Name = "Spring 2024",
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 4, 30),
                    Status = TermStatus.Active
                };
                _context.Terms.Add(term);

                var fptuStudents = await _context.Users
                    .Where(u => u.Role == UserRole.Student && (u.Email == "student1@fptu.edu.vn" || u.Email == "student2@fptu.edu.vn"))
                    .Join(_context.Students, u => u.UserId, s => s.UserId, (u, s) => s)
                    .ToListAsync();

                foreach (var s in fptuStudents)
                {
                    _context.StudentTerms.Add(new StudentTerm { StudentId = s.StudentId, TermId = term.TermId, Status = StudentTermStatus.Enrolled });
                }
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedInternshipGroups()
        {
            if (!await _context.InternshipGroups.AnyAsync())
            {
                var term = await _context.Terms.FirstOrDefaultAsync() ?? new Term
                {
                    TermId = Guid.NewGuid(),
                    Name = "Default Term",
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 12, 31),
                    Status = TermStatus.Active
                };
                if (term.CreatedAt == default) _context.Terms.Add(term);

                var fpt = await _context.Enterprises.FirstOrDefaultAsync(e => e.Name.Contains("FPT"))
                          ?? await _context.Enterprises.FirstOrDefaultAsync();

                var mentorUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "mentor.fpt@iocv2.com")
                                 ?? await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Mentor);

                var mentor = await _context.EnterpriseUsers.FirstOrDefaultAsync(eu => mentorUser != null && eu.UserId == mentorUser.UserId);

                var student1User = await _context.Users.FirstOrDefaultAsync(u => u.Email == "student1@fptu.edu.vn")
                                   ?? await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Student);
                var student1 = await _context.Students.FirstOrDefaultAsync(s => student1User != null && s.UserId == student1User.UserId);

                var student2User = await _context.Users.FirstOrDefaultAsync(u => u.Email == "student2@fptu.edu.vn")
                                   ?? await _context.Users.Where(u => u.Role == UserRole.Student).Skip(1).FirstOrDefaultAsync();
                var student2 = await _context.Students.FirstOrDefaultAsync(s => student2User != null && s.UserId == student2User.UserId);

                if (fpt != null && mentor != null && student1 != null)
                {
                    var group = InternshipGroup.Create(
                        term.TermId,
                        "Nhóm .NET Tiềm Năng 2026",
                        fpt.EnterpriseId,
                        mentor.EnterpriseUserId,
                        DateTime.UtcNow,
                        DateTime.UtcNow.AddMonths(2)
                    );
                    group.UpdateStatus(InternshipStatus.InProgress);

                    _context.InternshipGroups.Add(group);

                    var member1 = new InternshipStudent
                    {
                        InternshipId = group.InternshipId,
                        StudentId = student1.StudentId,
                        Role = InternshipRole.Leader,
                        Status = InternshipStatus.InProgress,
                        JoinedAt = DateTime.UtcNow
                    };
                    _context.InternshipStudents.Add(member1);

                    // Also seed an application for the leader
                    _context.InternshipApplications.Add(new InternshipApplication
                    {
                        ApplicationId = Guid.NewGuid(),
                        InternshipId = group.InternshipId,
                        StudentId = student1.StudentId,
                        Status = InternshipApplicationStatus.Approved,
                        AppliedAt = DateTime.UtcNow.AddDays(-40)
                    });

                    if (_context.ChangeTracker.HasChanges())
                    {
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
        private async Task SeedProjects()
        {
            if (!await _context.Projects.AnyAsync())
            {
                var group = await _context.InternshipGroups.FirstAsync();
                var project = Project.Create(
                    group.InternshipId,
                    "E-Commerce System IOC",
                    "Building a next-gen e-commerce platform");

                project.Update(null, null, null, DateTime.UtcNow.AddDays(-25), null, ProjectStatus.InProgress);

                _context.Projects.Add(project);

                _context.ProjectResources.Add(new ProjectResources
                {
                    ProjectResourceId = Guid.NewGuid(),
                    ProjectId = project.ProjectId,
                    ResourceName = "System Design Document",
                    ResourceUrl = "https://docs.example.com/design"
                });

                var sprint = new Sprint(project.ProjectId, "Sprint 1", "Initial sprint for seeding");
                sprint.Start(
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                    DateOnly.FromDateTime(DateTime.UtcNow.AddDays(4))
                );
                _context.Sprints.Add(sprint);

                var student = await _context.Students.FirstAsync();
                _context.WorkItems.Add(new WorkItem
                {
                    WorkItemId = Guid.NewGuid(),
                    ProjectId = project.ProjectId,
                    Title = "Setup Project Base",
                    Description = "Initialize repository and basic project structure",
                    Type = WorkItemType.Task,
                    Priority = Priority.High,
                    Status = WorkItemStatus.Done,
                    AssigneeId = student.StudentId,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-15))
                });

                _context.WorkItems.Add(new WorkItem
                {
                    WorkItemId = Guid.NewGuid(),
                    ProjectId = project.ProjectId,
                    Title = "Develop Login UI",
                    Description = "Implement login page with full design",
                    Type = WorkItemType.Task,
                    Priority = Priority.Medium,
                    Status = WorkItemStatus.InProgress,
                    AssigneeId = student.StudentId,
                    DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))
                });

                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedLogbooks()
        {
            if (!await _context.Logbooks.AnyAsync())
            {
                var project = await _context.Projects.FirstAsync();
                var student = await _context.Students.FirstAsync();

                var logbook = Logbook.Create(
                    project.InternshipId,  // FK → internship_groups.internship_id
                    student.StudentId,
                    "Finished login UI and integrated with API.",
                    null, // Issue
                    "Continue project development.",
                    DateTime.UtcNow);

                _context.Logbooks.Add(logbook);
                await _context.SaveChangesAsync();
            }


            if (_context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}

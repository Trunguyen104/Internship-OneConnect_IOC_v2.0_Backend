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
                        EnterpriseId = Guid.NewGuid(),
                        Name = "FPT Software",
                        TaxCode = "0101248141",
                        Industry = "Information Technology",
                        Address = "Hồ Chí Minh",
                        IsVerified = true,
                        Status = (short)EnterpriseStatus.Active
                    },
                    new Enterprise
                    {
                        EnterpriseId = Guid.NewGuid(),
                        Name = "Rikkeisoft",
                        TaxCode = "0100109106",
                        Industry = "Information Technology",
                        Address = "Hồ Chí Minh",
                        IsVerified = true,
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
                existingEmails.Add(superAdmin.Email);
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
                    existingEmails.Add(email);

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
                    existingEmails.Add(email);

                    _context.EnterpriseUsers.Add(new EnterpriseUser
                    {
                        EnterpriseUserId = Guid.NewGuid(),
                        UserId = user.UserId,
                        EnterpriseId = ent.EnterpriseId,
                        Position = "HR Manager"
                    });
                }
            }

            // 6. Test Student (Fixed account for dev)
            var testStudentEmail = "trunguyen.104@gmail.com";
            if (!existingEmails.Contains(testStudentEmail))
            {
                var userId = Guid.NewGuid();
                var userCode = await _userService.GenerateUserCodeAsync(UserRole.Student, cancellationToken);
                var user = new User(
                    userId,
                    userCode,
                    testStudentEmail,
                    "Nguyễn Trung Nguyên",
                    UserRole.Student,
                    passHash
                );
                user.SetStatus(UserStatus.Active);
                _context.Users.Add(user);
                existingEmails.Add(testStudentEmail);
                
                var uni = universityList.FirstOrDefault(u => u.Code == "FPTU") ?? universityList.First();
                _context.UniversityUsers.Add(new UniversityUser { UniversityUserId = Guid.NewGuid(), UserId = user.UserId, UniversityId = uni.UniversityId });
                _context.Students.Add(new Student
                {
                    StudentId = Guid.NewGuid(),
                    UserId = user.UserId,
                    InternshipStatus = StudentStatus.INTERNSHIP_IN_PROGRESS,
                    Major = "Software Engineering",
                    ClassName = "SE1616"
                });
            }

            // 6. Students Loop
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
                        existingEmails.Add(email);
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
                        $"Trần Doãn Đô",
                        UserRole.Mentor,
                        passHash
                    );
                    user.SetStatus(UserStatus.Active);
                    _context.Users.Add(user);
                    existingEmails.Add(email);
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
                    Name = "Spring 2026",
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 4, 30),
                    Status = TermStatus.Open
                };
                _context.Terms.Add(term);

                var students = await _context.Students.Take(2).ToListAsync();
                foreach (var s in students)
                {
                    _context.StudentTerms.Add(new StudentTerm { StudentTermId = Guid.NewGuid(), StudentId = s.StudentId, TermId = term.TermId, EnrollmentStatus = EnrollmentStatus.Active, PlacementStatus = PlacementStatus.Unplaced, EnrollmentDate = DateOnly.FromDateTime(DateTime.UtcNow) });

                }
                await _context.SaveChangesAsync();
            }
        }

        private async Task SeedInternshipGroups()
        {
            var testStudentEmail = "trunguyen.104@gmail.com";
            var studentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == testStudentEmail);
            
            if (studentUser != null)
            {
                var student = await _context.Students.Include(s => s.InternshipStudents).FirstOrDefaultAsync(s => s.UserId == studentUser.UserId);
                
                if (student != null && !student.InternshipStudents.Any())
                {
                    var term = await _context.Terms.OrderByDescending(t => t.StartDate).FirstOrDefaultAsync();
                    var fpt = await _context.Enterprises.FirstOrDefaultAsync(e => e.Name.Contains("FPT")) 
                              ?? await _context.Enterprises.FirstOrDefaultAsync();
                    var mentor = await _context.EnterpriseUsers.FirstOrDefaultAsync(eu => 
                                        eu.Position != null && (eu.Position.Contains("Senior") || eu.Position.Contains("Mentor")))
                                   ?? await _context.EnterpriseUsers.FirstOrDefaultAsync();

                    if (term != null && fpt != null && mentor != null)
                    {
                        var group = InternshipGroup.Create(
                            term.TermId,
                            "Nhóm OJT .NET WebCore - Rikkeisoft",
                            fpt.EnterpriseId,
                            mentor.EnterpriseUserId,
                            DateTime.UtcNow.AddMonths(-1),
                            DateTime.UtcNow.AddMonths(3)
                        );
                        group.UpdateStatus(InternshipStatus.InProgress);
                        _context.InternshipGroups.Add(group);

                        var member = new InternshipStudent
                        {
                            InternshipId = group.InternshipId,
                            StudentId = student.StudentId,
                            Role = InternshipRole.Leader,
                            Status = InternshipStatus.InProgress,
                            JoinedAt = DateTime.UtcNow.AddMonths(-1)
                        };
                        _context.InternshipStudents.Add(member);
                        
                         _context.InternshipApplications.Add(new InternshipApplication
                        {
                            ApplicationId = Guid.NewGuid(),
                            InternshipId = group.InternshipId,
                            StudentId = student.StudentId,
                            Status = InternshipApplicationStatus.Approved,
                            AppliedAt = DateTime.UtcNow.AddDays(-40)
                        });

                        await _context.SaveChangesAsync();
                        System.Console.WriteLine($"Seeded Internship Group for {testStudentEmail}");
                    }
                }
            }

            // Fallback for generic data if necessary
            if (!await _context.InternshipGroups.AnyAsync())
            {
                 var term = await _context.Terms.FirstOrDefaultAsync();
                 var ent = await _context.Enterprises.FirstOrDefaultAsync();
                 var mentor = await _context.EnterpriseUsers.FirstOrDefaultAsync();
                 var student = await _context.Students.FirstOrDefaultAsync();

                 if (term != null && ent != null && mentor != null && student != null)
                 {
                    var group = InternshipGroup.Create(term.TermId, "Nhóm Thực Tập Mẫu", ent.EnterpriseId, mentor.EnterpriseUserId, DateTime.UtcNow, DateTime.UtcNow.AddMonths(3));
                    _context.InternshipGroups.Add(group);
                    _context.InternshipStudents.Add(new InternshipStudent { InternshipId = group.InternshipId, StudentId = student.StudentId, Role = InternshipRole.Member, Status = InternshipStatus.InProgress });
                    await _context.SaveChangesAsync();
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

                var studentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "trunguyen.104@gmail.com");
                var student = await _context.Students.FirstOrDefaultAsync(s => studentUser != null && s.UserId == studentUser.UserId) 
                              ?? await _context.Students.FirstAsync();

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
                var project = await _context.Projects.OrderByDescending(p => p.CreatedAt).FirstAsync();
                var studentUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "trunguyen.104@gmail.com");
                var student = await _context.Students.FirstOrDefaultAsync(s => studentUser != null && s.UserId == studentUser.UserId) 
                              ?? await _context.Students.FirstAsync();

                var logbook = Logbook.Create(
                    project.InternshipId,  
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

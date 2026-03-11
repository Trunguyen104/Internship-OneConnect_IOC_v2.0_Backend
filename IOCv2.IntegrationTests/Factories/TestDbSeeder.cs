using System;
using System.Collections.Generic;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using IOCv2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace IOCv2.IntegrationTests.Factories;

public static class TestDbSeeder
{
    public static void SeedTestData(AppDbContext context, IPasswordService passwordService)
    {
        // Only seed if empty
        if (context.Universities.Any())
            return;

        // 1. Seed Universities
        var fptuId = Guid.NewGuid();
        context.Universities.Add(new University
        {
            UniversityId = fptuId,
            Code = "FPTU",
            Name = "FPT University Test",
            Address = "Test Address",
            Status = 1
        });

        // 2. Seed Enterprises
        var fptSoftId = Guid.NewGuid();
        context.Enterprises.Add(new Enterprise
        {
            EnterpriseId = fptSoftId,
            Name = "FPT Software Test",
            TaxCode = "0101248141",
            Industry = "IT Test",
            Address = "Test Address",
            IsVerified = true,
            Status = 1
        });

        context.SaveChanges();

        // 3. Seed Users
        var passHash = passwordService.HashPassword("Admin@123");

        // Super Admin
        var superAdmin = new User(
            Guid.NewGuid(),
            "SA-001",
            "admin@iocv2.com",
            "Super Admin Test",
            UserRole.SuperAdmin,
            passHash
        );
        superAdmin.SetStatus(UserStatus.Active);
        context.Users.Add(superAdmin);

        // Enterprise Admin
        var enterpriseAdmin = new User(
            Guid.NewGuid(),
            "EA-001",
            "admin@fpt.com",
            "Enterprise Admin Test",
            UserRole.EnterpriseAdmin,
            passHash
        );
        enterpriseAdmin.SetStatus(UserStatus.Active);
        context.Users.Add(enterpriseAdmin);

        context.EnterpriseUsers.Add(new EnterpriseUser
        {
            EnterpriseUserId = Guid.NewGuid(),
            UserId = enterpriseAdmin.UserId,
            EnterpriseId = fptSoftId,
            Position = "Admin"
        });

        // Mentor
        var mentor = new User(
            Guid.NewGuid(),
            "ME-001",
            "mentor@fpt.com",
            "Mentor Test",
            UserRole.Mentor,
            passHash
        );
        mentor.SetStatus(UserStatus.Active);
        context.Users.Add(mentor);
        
        var mentorEuId = Guid.NewGuid();
        context.EnterpriseUsers.Add(new EnterpriseUser
        {
            EnterpriseUserId = mentorEuId,
            UserId = mentor.UserId,
            EnterpriseId = fptSoftId,
            Position = "Senior Dev"
        });

        // Student
        var studentUser = new User(
            Guid.NewGuid(),
            "ST-001",
            "student@fptu.edu.vn",
            "Student Test",
            UserRole.Student,
            passHash
        );
        studentUser.SetStatus(UserStatus.Active);
        context.Users.Add(studentUser);

        context.UniversityUsers.Add(new UniversityUser
        {
            UniversityUserId = Guid.NewGuid(),
            UserId = studentUser.UserId,
            UniversityId = fptuId
        });

        var studentEntityId = Guid.NewGuid();
        context.Students.Add(new Student
        {
            StudentId = studentEntityId,
            UserId = studentUser.UserId,
            InternshipStatus = StudentStatus.NO_INTERNSHIP,
            Major = "Computer Science",
            ClassName = "SE1600"
        });

        context.SaveChanges();

        // 4. Terms and Groups
        var termId = Guid.NewGuid();
        var term = new Term
        {
            TermId = termId,
            UniversityId = fptuId,
            Name = "Test Term 2026",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 5, 1),
            Status = TermStatus.Open
        };
        context.Terms.Add(term);

        context.StudentTerms.Add(new StudentTerm
        {
            StudentId = studentEntityId,
            TermId = termId,
            Status = 1
        });
        
        context.SaveChanges();
        
        var groupId = Guid.NewGuid();
        var group = InternshipGroup.Create(
            termId,
            "Test Internship Group",
            fptSoftId,
            mentorEuId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(2)
        );
        // Workaround to set identity column if any, but since it's guid we can force set Id or let it generate
        // The Create method returns new Group.
        context.InternshipGroups.Add(group);
        
        context.SaveChanges();
    }
}

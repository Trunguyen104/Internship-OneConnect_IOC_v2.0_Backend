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
        context.Universities.Add(University.Create("FPTU", "FPT University Test", "Test Address", null, fptuId));

        // 2. Seed Enterprises
        var fptSoftId = Guid.NewGuid();
        context.Enterprises.Add(new Enterprise
        {
            EnterpriseId = fptSoftId,
            Name = "FPT Software Test",
            TaxCode = "0101248141",
            Industry = "IT Test",
            Address = "Test Address",
            Status = EnterpriseStatus.Active
        });

        context.SaveChanges();

        // 3. Seed Users
        var passHash = passwordService.HashPassword("Admin@123");

        // Super Admin
        var superAdminId = Guid.NewGuid();
        var superAdmin = new User(
            superAdminId,
            "SA-001",
            "admin@iocv2.com",
            "Super Admin Test",
            UserRole.SuperAdmin,
            passHash
        );
        superAdmin.SetStatus(UserStatus.Active);
        context.Users.Add(superAdmin);

        // Enterprise Admin
        var enterpriseAdminId = Guid.NewGuid();
        var enterpriseAdmin = new User(
            enterpriseAdminId,
            "EA-001",
            "admin@fpt.com",
            "Enterprise Admin Test",
            UserRole.EnterpriseAdmin,
            passHash
        );
        enterpriseAdmin.SetStatus(UserStatus.Active);
        context.Users.Add(enterpriseAdmin);

        var entUser = new EnterpriseUser
        {
            EnterpriseUserId = Guid.NewGuid(),
            UserId = enterpriseAdminId,
            EnterpriseId = fptSoftId
        };
        entUser.UpdateMetadata("Admin", null, null);
        context.EnterpriseUsers.Add(entUser);

        // Mentor
        var mentorUserId = Guid.NewGuid();
        var mentor = new User(
            mentorUserId,
            "ME-001",
            "mentor@fpt.com",
            "Mentor Test",
            UserRole.Mentor,
            passHash
        );
        mentor.SetStatus(UserStatus.Active);
        context.Users.Add(mentor);
        
        var mentorEuId = Guid.NewGuid();
        var mentorEu = new EnterpriseUser
        {
            EnterpriseUserId = mentorEuId,
            UserId = mentorUserId,
            EnterpriseId = fptSoftId
        };
        mentorEu.UpdateMetadata("Senior Dev", null, null);
        context.EnterpriseUsers.Add(mentorEu);

        // Student
        var studentUserId = Guid.NewGuid();
        var studentUser = new User(
            studentUserId,
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
            UserId = studentUserId,
            UniversityId = fptuId
        });

        var studentEntityId = Guid.NewGuid();
        context.Students.Add(new Student
        {
            StudentId = studentEntityId,
            UserId = studentUserId,
            InternshipStatus = StudentStatus.NO_INTERNSHIP,
            Major = "Computer Science",
            ClassName = "SE1600"
        });

        context.SaveChanges();

        // 4. Terms, Phases and Groups
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

        var phase = InternshipPhase.Create(
            fptSoftId, 
            "Test Phase",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(90)),
            "CNTT",
            15,
            "Test phase for integration tests",
            null);

        context.InternshipPhases.Add(phase);

        context.StudentTerms.Add(new StudentTerm
        {
            StudentTermId = Guid.NewGuid(),
            StudentId = studentEntityId,
            TermId = termId,
            EnrollmentStatus = EnrollmentStatus.Active,
            PlacementStatus = PlacementStatus.Unplaced,
            EnrollmentDate = new DateOnly(2026, 1, 1)
        });
        
        context.SaveChanges();
        
        var group = InternshipGroup.Create(
            phase.PhaseId,
            "Test Internship Group",
            "Test Description",
            fptSoftId,
            mentorEuId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(2)
        );
        context.InternshipGroups.Add(group);
        
        context.SaveChanges();
    }
}

using System;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using IOCv2.Infrastructure.Persistence;
using IOCv2.IntegrationTests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IOCv2.IntegrationTests.Features.Projects;

public class GetProjectTests : BaseIntegrationTest
{
    public GetProjectTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProject_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        var projectId = Guid.NewGuid();
        // Act
        var response = await Client.GetAsync($"/api/v1/projects/{projectId}");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProject_ShouldReturnOk_WhenUserIsAuthenticated()
    {
        // Arrange: Authenticate and get DB context
        await AuthenticateAsUserAsync("mentor@fptsoftware.com", "Admin@123");

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Get a seeded InternshipId (required to create a Project)
        var internshipId = await db.InternshipGroups
            .Select(g => g.InternshipId)
            .FirstOrDefaultAsync();

        if (internshipId == Guid.Empty)
        {
            throw new Exception("No InternshipGroups found. Ensure TestDbSeeder is working.");
        }

        // Get the mentor details
        var mentorUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "mentor@fptsoftware.com");
        var mentorEnterpriseUser = await db.EnterpriseUsers.FirstOrDefaultAsync(eu => eu.UserId == mentorUser!.UserId);

        // Create and save a new Project
        var project = IOCv2.Domain.Entities.Project.Create(
            "Integration Test Project",
            "Test Description",
            "PRJ-INT-GETPROJ-1",
            "IT",
            "Integration test requirements",
            mentorId: mentorEnterpriseUser?.EnterpriseUserId
        );
        project.Publish();
        project.AssignToGroup(internshipId, null, null);
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/v1/projects/{project.ProjectId}");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        response.IsSuccessStatusCode.Should().BeTrue("Status: {0}, Content: {1}", response.StatusCode, content);
    }

    [Fact]
    public async Task GetProject_ShouldIncludeProjectResourceId_ForEachResource()
    {
        await AuthenticateAsUserAsync("mentor@fptsoftware.com", "Admin@123");

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var internshipId = await db.InternshipGroups
            .Select(g => g.InternshipId)
            .FirstOrDefaultAsync();

        internshipId.Should().NotBe(Guid.Empty, "seed data must include internship groups");

        var mentorUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "mentor@fptsoftware.com");
        mentorUser.Should().NotBeNull();

        var mentorEnterpriseUser = await db.EnterpriseUsers.FirstOrDefaultAsync(eu => eu.UserId == mentorUser!.UserId);
        mentorEnterpriseUser.Should().NotBeNull();

        var project = Project.Create(
            "Integration Resource Id Project",
            "Validate GET response includes projectResourceId",
            "PRJ-INT-GETRES-1",
            "IT",
            "Integration test requirements",
            mentorId: mentorEnterpriseUser!.EnterpriseUserId
        );
        project.Publish();
        project.AssignToGroup(internshipId, null, null);

        var linkResource = new ProjectResources(project.ProjectId, "Spec Link", FileType.LINK, "https://example.com/spec")
        {
            ProjectResourceId = Guid.NewGuid()
        };

        db.Projects.Add(project);
        db.ProjectResources.Add(linkResource);
        await db.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/v1/projects/{project.ProjectId}");
        var content = await response.Content.ReadAsStringAsync();

        response.IsSuccessStatusCode.Should().BeTrue("Status: {0}, Content: {1}", response.StatusCode, content);

        using var json = JsonDocument.Parse(content);
        var root = json.RootElement;
        root.TryGetProperty("data", out var data).Should().BeTrue();
        data.TryGetProperty("projectResources", out var resources).Should().BeTrue();
        resources.ValueKind.Should().Be(JsonValueKind.Array);
        resources.GetArrayLength().Should().BeGreaterThan(0);

        var firstResource = resources.EnumerateArray().First();
        firstResource.TryGetProperty("projectResourceId", out var projectResourceId).Should().BeTrue();
        projectResourceId.GetGuid().Should().NotBe(Guid.Empty);
    }
}
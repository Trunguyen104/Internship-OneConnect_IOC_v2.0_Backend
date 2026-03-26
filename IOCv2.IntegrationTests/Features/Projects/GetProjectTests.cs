using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
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
        var request = new GetProjectByIdQuery
        {
            ProjectId = Guid.NewGuid()
        };
        // Act
        var response = await Client.GetAsync($"/api/v1/projects/{request.ProjectId}");

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

        // Create and save a new Project
        var project = IOCv2.Domain.Entities.Project.Create(
            internshipId,
            "Integration Test Project",
            "Test Description",
            "PRJ-INT-GETPROJ-1",
            "IT",
            "Integration test requirements"
        );
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/v1/projects/{project.ProjectId}");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        response.IsSuccessStatusCode.Should().BeTrue("Status: {0}, Content: {1}", response.StatusCode, content);
    }
}
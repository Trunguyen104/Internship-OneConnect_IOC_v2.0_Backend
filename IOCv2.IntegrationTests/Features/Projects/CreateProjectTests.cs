using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Infrastructure.Persistence;
using IOCv2.IntegrationTests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IOCv2.IntegrationTests.Features.Projects;

/// <summary>
/// Component/Integration tests for creating a project
/// </summary>
public class CreateProjectTests : BaseIntegrationTest
{
    public CreateProjectTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateProject_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = new CreateProjectCommand
        {
            ProjectName = "New Test Project Unauthenticated",
            Description = "Test description",
            InternshipId = Guid.NewGuid(),
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects", request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task CreateProject_ShouldReturnSuccess_WhenUserIsAuthenticated()
    {
        // Arrange
        await AuthenticateAsUserAsync("mentor@fpt.com", "Admin@123");

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var internshipId = await db.InternshipGroups.Select(g => g.InternshipId).FirstOrDefaultAsync();

        var request = new CreateProjectCommand
        {
            ProjectName = "New Test Project Authenticated",
            Description = "Test description",
            InternshipId = internshipId, // Use real seeded internship ID
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1)
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects", request);

        // Assert
        // The exact expected status might be Created (201) or OK (200) depending on your controller implementation.
        // Replace with HttpStatusCode.Created if that's what your CreateProject endpoint produces.
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        response.IsSuccessStatusCode.Should().BeTrue("Status: {0}, Content: {1}", response.StatusCode, content);
    }
}

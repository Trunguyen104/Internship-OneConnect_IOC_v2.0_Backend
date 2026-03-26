using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using IOCv2.Domain.Enums;
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
        using var request = CreateCreateProjectMultipartContent(
            projectName: "New Test Project Unauthenticated",
            internshipId: Guid.NewGuid(),
            startDate: DateTime.UtcNow,
            endDate: DateTime.UtcNow.AddMonths(1));

        // Act
        var response = await Client.PostAsync("/api/v1/projects", request);

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task CreateProject_ShouldReturnSuccess_WhenUserIsAuthenticated()
    {
        // Arrange
        await AuthenticateAsUserAsync("mentor@fptsoftware.com", "Admin@123");

        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var internshipId = await db.InternshipGroups
            .Where(g => g.Status == GroupStatus.Active)
            .Select(g => g.InternshipId)
            .FirstOrDefaultAsync();

        using var request = CreateCreateProjectMultipartContent(
            projectName: "New Test Project Authenticated",
            internshipId: internshipId,
            startDate: DateTime.UtcNow,
            endDate: DateTime.UtcNow.AddMonths(1),
            field: "Công nghệ thông tin",
            requirements: "Yêu cầu dự án kiểm thử tích hợp.");

        // Act
        var response = await Client.PostAsync("/api/v1/projects", request);

        // Assert
        // The exact expected status might be Created (201) or OK (200) depending on your controller implementation.
        // Replace with HttpStatusCode.Created if that's what your CreateProject endpoint produces.
        var content = await response.Content.ReadAsStringAsync();
        Console.WriteLine(content);
        response.IsSuccessStatusCode.Should().BeTrue("Status: {0}, Content: {1}", response.StatusCode, content);
    }

    private static MultipartFormDataContent CreateCreateProjectMultipartContent(
        string projectName,
        Guid? internshipId,
        DateTime startDate,
        DateTime endDate,
        string description = "Test description",
        string field = "Information Technology",
        string requirements = "Integration test requirements")
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(projectName), "ProjectName");
        content.Add(new StringContent(description), "Description");
        content.Add(new StringContent(field), "Field");
        content.Add(new StringContent(requirements), "Requirements");
        content.Add(new StringContent(startDate.ToString("O")), "StartDate");
        content.Add(new StringContent(endDate.ToString("O")), "EndDate");

        if (internshipId.HasValue)
        {
            content.Add(new StringContent(internshipId.Value.ToString()), "InternshipId");
        }

        return content;
    }
}

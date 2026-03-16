using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IOCv2.Application.Features.Projects.Queries.GetAllProjects;
using IOCv2.Infrastructure.Persistence;
using IOCv2.IntegrationTests.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IOCv2.IntegrationTests.Features.Projects;

public class GetProjectsTests : BaseIntegrationTest
{
    public GetProjectsTests(TestWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProjects_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/v1/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjects_ShouldReturnOk_WhenUserIsAuthenticated() 
    {
        // Arrange
        await AuthenticateAsUserAsync("mentor@fpt.com", "Admin@123");

        // Act
        var response = await Client.GetAsync("/api/v1/projects");

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    
    [Fact]
    public async Task GetProjects_ShouldReturnFilteredResults_WhenStatusIsProvided()
    {
        // Arrange
        await AuthenticateAsUserAsync("mentor@fpt.com", "Admin@123");
        var status = 1; // e.g., Planning

        // Act - Correct way to pass query parameters in integration tests
        var response = await Client.GetAsync($"/api/v1/projects?status={status}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

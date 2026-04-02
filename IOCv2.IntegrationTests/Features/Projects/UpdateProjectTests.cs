using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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

namespace IOCv2.IntegrationTests.Features.Projects
{
    public class UpdateProjectTests : BaseIntegrationTest
    {
        public UpdateProjectTests(TestWebApplicationFactory factory) : base(factory) {
        }

        [Fact]
        public async Task UpdateProject_ShouldDeleteResource_WhenUsingResourceIdReturnedFromGetProjectById()
        {
            await AuthenticateAsUserAsync("mentor@fptsoftware.com", "Admin@123");

            Guid projectId;
            Guid seededResourceId;

            using (var scope = CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var mentorUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "mentor@fptsoftware.com");
                mentorUser.Should().NotBeNull();

                var mentorEnterpriseUser = await db.EnterpriseUsers.FirstOrDefaultAsync(eu => eu.UserId == mentorUser!.UserId);
                mentorEnterpriseUser.Should().NotBeNull();

                var project = Project.Create(
                    "Integration Update Resource Delete",
                    "resource delete flow",
                    "PRJ-INT-UPD-DEL-1",
                    "IT",
                    "Integration test requirements",
                    mentorId: mentorEnterpriseUser!.EnterpriseUserId
                );

                var resource = new ProjectResources(project.ProjectId, "Old Link", FileType.LINK, "https://example.com/old")
                {
                    ProjectResourceId = Guid.NewGuid()
                };

                db.Projects.Add(project);
                db.ProjectResources.Add(resource);
                await db.SaveChangesAsync();

                projectId = project.ProjectId;
                seededResourceId = resource.ProjectResourceId;
            }

            var getResponse = await Client.GetAsync($"/api/v1/projects/{projectId}");
            var getContent = await getResponse.Content.ReadAsStringAsync();
            getResponse.IsSuccessStatusCode.Should().BeTrue("Status: {0}, Content: {1}", getResponse.StatusCode, getContent);

            using var getJson = JsonDocument.Parse(getContent);
            var resourceFromApi = getJson.RootElement
                .GetProperty("data")
                .GetProperty("projectResources")
                .EnumerateArray()
                .Select(x => x.GetProperty("projectResourceId").GetGuid())
                .FirstOrDefault();

            resourceFromApi.Should().NotBe(Guid.Empty);
            resourceFromApi.Should().Be(seededResourceId);

            using var form = new MultipartFormDataContent
            {
                { new StringContent("Updated Name"), "ProjectName" },
                { new StringContent(resourceFromApi.ToString()), "ResourceDeleteIds" }
            };

            var updateResponse = await Client.PutAsync($"/api/v1/projects/{projectId}", form);
            var updateContent = await updateResponse.Content.ReadAsStringAsync();

            updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Content: {0}", updateContent);

            using (var verifyScope = CreateScope())
            {
                var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
                var stillExists = await verifyDb.ProjectResources
                    .AsNoTracking()
                    .AnyAsync(r => r.ProjectResourceId == resourceFromApi);

                stillExists.Should().BeFalse("resource should be deleted by UpdateProject using ResourceDeleteIds");
            }
        }
    }
}
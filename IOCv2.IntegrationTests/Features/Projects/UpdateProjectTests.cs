using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using IOCv2.Application.Features.Projects.Commands.UpdateProject;
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
        public async Task UpdateProject_ValidRequest_ReturnsSuccessStatusCode()
        {
           
        }
    }
}
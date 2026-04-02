using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using Microsoft.Extensions.Logging;

namespace IOCv2.Tests.Features.Projects.Queries;

public class GetProjectByIdHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCached_WhenCacheHit()
    {
        var projectId = Guid.NewGuid();
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<GetProjectByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetProjectByIdResponse { ProjectName = "Cached Project" });

        var project = Project.Create("Test", "Test", "Test", "Test", "Test");
        var propertyInfo = typeof(Project).GetProperty("ProjectId");
        propertyInfo?.SetValue(project, projectId);
        project.Publish();

        var repo = new Mock<IGenericRepository<Project>>();
        var projects = new List<Project> { project }.AsQueryable().BuildMock();
        repo.Setup(x => x.Query()).Returns(projects);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Project>()).Returns(repo.Object);

        var handler = new GetProjectByIdHandler(
            uow.Object,
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<GetProjectByIdHandler>>(),
            Mock.Of<IMessageService>(),
            cache.Object,
            Mock.Of<ICurrentUserService>());

        var result = await handler.Handle(new GetProjectByIdQuery { ProjectId = projectId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ProjectName.Should().Be("Cached Project");
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenEntityMissing()
    {
        var repo = new Mock<IGenericRepository<Project>>();
        repo.Setup(x => x.Query()).Returns(new List<Project>().AsQueryable().BuildMock());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Project>()).Returns(repo.Object);

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<GetProjectByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetProjectByIdResponse?)null);

        var message = new Mock<IMessageService>();
        message.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Project not found");

        var handler = new GetProjectByIdHandler(
            uow.Object,
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<GetProjectByIdHandler>>(),
            message.Object,
            cache.Object);

        var result = await handler.Handle(new GetProjectByIdQuery { ProjectId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}

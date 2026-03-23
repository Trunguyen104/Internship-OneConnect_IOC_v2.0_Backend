using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;

namespace IOCv2.Tests.Features.ProjectResources.Commands;

public class UploadProjectResourceHandlerTests
{
    [Fact]
    public async Task Handle_SavesExternalLink_WhenExternalUrlProvided()
    {
        var projectId = Guid.NewGuid();
        var internshipId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        var project = Project.Create(internshipId, "Demo", string.Empty);
        var projectRepo = new Mock<IGenericRepository<Project>>();
        projectRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        projectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());

        var studentRepo = new Mock<IGenericRepository<Student>>();
        studentRepo.Setup(x => x.Query()).Returns(new List<Student> { new() { StudentId = studentId, UserId = userId } }.AsQueryable().BuildMock());

        var memberRepo = new Mock<IGenericRepository<InternshipStudent>>();
        memberRepo.Setup(x => x.Query()).Returns(new List<InternshipStudent> { new() { InternshipId = internshipId, StudentId = studentId } }.AsQueryable().BuildMock());

        var resourceRepo = new Mock<IGenericRepository<IOCv2.Domain.Entities.ProjectResources>>();

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<Project>()).Returns(projectRepo.Object);
        uow.Setup(x => x.Repository<Student>()).Returns(studentRepo.Object);
        uow.Setup(x => x.Repository<InternshipStudent>()).Returns(memberRepo.Object);
        uow.Setup(x => x.Repository<IOCv2.Domain.Entities.ProjectResources>()).Returns(resourceRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<UploadProjectResourceResponse>(It.IsAny<IOCv2.Domain.Entities.ProjectResources>()))
            .Returns((IOCv2.Domain.Entities.ProjectResources src) => new UploadProjectResourceResponse
            {
                ProjectResourceId = src.ProjectResourceId,
                ProjectId = src.ProjectId,
                ResourceName = src.ResourceName ?? string.Empty,
                ResourceType = src.ResourceType,
                ResourceUrl = src.ResourceUrl
            });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId.ToString());
        currentUser.Setup(x => x.Role).Returns("Student");

        var message = new Mock<IMessageService>();
        message.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("ok");

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new UploadProjectResourceHandler(
            uow.Object,
            mapper.Object,
            Mock.Of<ILogger<UploadProjectResourceHandler>>(),
            Mock.Of<IFileStorageService>(),
            currentUser.Object,
            message.Object,
            cache.Object);

        var result = await handler.Handle(new UploadProjectResourceCommand
        {
            ProjectId = project.ProjectId,
            ResourceName = "Figma",
            ResourceType = FileType.LINK,
            ExternalUrl = "https://figma.com/file/abc"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ResourceType.Should().Be(FileType.LINK);
        result.Data.ResourceUrl.Should().Be("https://figma.com/file/abc");
        resourceRepo.Verify(x => x.AddAsync(It.IsAny<IOCv2.Domain.Entities.ProjectResources>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

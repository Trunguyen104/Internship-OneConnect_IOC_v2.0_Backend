using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace IOCv2.Tests.Features.ProjectResources.Commands;

public class UpdateProjectResourceHandlerTests
{
    [Fact]
    public async Task Handle_ProjectMismatch_ShouldReturnBadRequest()
    {
        var resourceId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var differentProjectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var internshipId = Guid.NewGuid();

        var resource = new IOCv2.Domain.Entities.ProjectResources(projectId, "avatar.png", FileType.PNG, "/uploads/a.png")
        {
            ProjectResourceId = resourceId
        };

        var project = Project.Create("Demo", string.Empty, "PRJ-DEMO_DMO_3", "IT", "Requirements");
        project.AssignToGroup(internshipId, null, null);

        var resourceRepo = new Mock<IGenericRepository<IOCv2.Domain.Entities.ProjectResources>>();
        resourceRepo.Setup(x => x.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);

        var projectRepo = new Mock<IGenericRepository<Project>>();
        projectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());

        var studentRepo = new Mock<IGenericRepository<Student>>();
        studentRepo.Setup(x => x.Query()).Returns(new List<Student> { new() { StudentId = studentId, UserId = userId } }.AsQueryable().BuildMock());

        var memberRepo = new Mock<IGenericRepository<InternshipStudent>>();
        memberRepo.Setup(x => x.Query()).Returns(new List<InternshipStudent> { new() { InternshipId = internshipId, StudentId = studentId } }.AsQueryable().BuildMock());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<IOCv2.Domain.Entities.ProjectResources>()).Returns(resourceRepo.Object);
        uow.Setup(x => x.Repository<Project>()).Returns(projectRepo.Object);
        uow.Setup(x => x.Repository<Student>()).Returns(studentRepo.Object);
        uow.Setup(x => x.Repository<InternshipStudent>()).Returns(memberRepo.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId.ToString());
        currentUser.Setup(x => x.Role).Returns("Admin");

        var message = new Mock<IMessageService>();
        message.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("invalid request");

        var cache = new Mock<ICacheService>();

        var handler = new UpdateProjectResourceHandler(
            Mock.Of<ILogger<UpdateProjectResourceHandler>>(),
            uow.Object,
            Mock.Of<AutoMapper.IMapper>(),
            message.Object,
            currentUser.Object,
            cache.Object);

        var result = await handler.Handle(new UpdateProjectResourceCommand
        {
            ProjectResourceId = resourceId,
            ProjectId = differentProjectId,
            ResourceName = "renamed",
            ResourceType = FileType.JPG
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        resource.ResourceType.Should().Be(FileType.PNG);
    }

    [Fact]
    public async Task Handle_FileExtensionChangeAttempt_ShouldReturnBadRequest()
    {
        var resourceId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var internshipId = Guid.NewGuid();

        var resource = new IOCv2.Domain.Entities.ProjectResources(projectId, "avatar.png", FileType.PNG, "/uploads/avatar.png")
        {
            ProjectResourceId = resourceId
        };

        var project = Project.Create("Demo", string.Empty, "PRJ-DEMO_DMO_4", "IT", "Requirements");
        project.AssignToGroup(internshipId, null, null);

        var resourceRepo = new Mock<IGenericRepository<IOCv2.Domain.Entities.ProjectResources>>();
        resourceRepo.Setup(x => x.GetByIdAsync(resourceId, It.IsAny<CancellationToken>())).ReturnsAsync(resource);

        var projectRepo = new Mock<IGenericRepository<Project>>();
        projectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());

        var studentRepo = new Mock<IGenericRepository<Student>>();
        studentRepo.Setup(x => x.Query()).Returns(new List<Student> { new() { StudentId = studentId, UserId = userId } }.AsQueryable().BuildMock());

        var memberRepo = new Mock<IGenericRepository<InternshipStudent>>();
        memberRepo.Setup(x => x.Query()).Returns(new List<InternshipStudent> { new() { InternshipId = internshipId, StudentId = studentId } }.AsQueryable().BuildMock());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<IOCv2.Domain.Entities.ProjectResources>()).Returns(resourceRepo.Object);
        uow.Setup(x => x.Repository<Project>()).Returns(projectRepo.Object);
        uow.Setup(x => x.Repository<Student>()).Returns(studentRepo.Object);
        uow.Setup(x => x.Repository<InternshipStudent>()).Returns(memberRepo.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId.ToString());
        currentUser.Setup(x => x.Role).Returns("Admin");

        var message = new Mock<IMessageService>();
        message.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("invalid request");

        var cache = new Mock<ICacheService>();

        var handler = new UpdateProjectResourceHandler(
            Mock.Of<ILogger<UpdateProjectResourceHandler>>(),
            uow.Object,
            Mock.Of<AutoMapper.IMapper>(),
            message.Object,
            currentUser.Object,
            cache.Object);

        var result = await handler.Handle(new UpdateProjectResourceCommand
        {
            ProjectResourceId = resourceId,
            ProjectId = projectId,
            ResourceName = "avatar.jpg"
        }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        resource.ResourceName.Should().Be("avatar.png");
    }
}



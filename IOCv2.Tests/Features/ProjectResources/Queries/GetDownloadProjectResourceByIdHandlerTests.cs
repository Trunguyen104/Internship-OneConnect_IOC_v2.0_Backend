using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetDownloadProjectResourceById;

namespace IOCv2.Tests.Features.ProjectResources.Queries;

public class GetDownloadProjectResourceByIdHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsBadRequest_WhenResourceTypeIsLink()
    {
        var resourceId = Guid.NewGuid();
        var internshipId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var project = Project.Create("Demo", string.Empty, "PRJ-DEMO_DMO_1", "IT", "Requirements");
        project.AssignToGroup(internshipId, null, null);
        var projectId = project.ProjectId;

        var resource = new IOCv2.Domain.Entities.ProjectResources(projectId, "Figma", FileType.LINK, "https://figma.com/file/abc")
        {
            ProjectResourceId = resourceId
        };

        var resources = new List<IOCv2.Domain.Entities.ProjectResources> { resource };
        var projects = new List<Project> { project };
        var students = new List<Student> { new() { StudentId = studentId, UserId = userId } };
        var memberships = new List<InternshipStudent> { new() { InternshipId = internshipId, StudentId = studentId } };

        var resourceRepo = new Mock<IGenericRepository<IOCv2.Domain.Entities.ProjectResources>>();
        resourceRepo.Setup(x => x.Query()).Returns(resources.AsQueryable().BuildMock());

        var projectRepo = new Mock<IGenericRepository<Project>>();
        projectRepo.Setup(x => x.Query()).Returns(projects.AsQueryable().BuildMock());

        var studentRepo = new Mock<IGenericRepository<Student>>();
        studentRepo.Setup(x => x.Query()).Returns(students.AsQueryable().BuildMock());

        var membershipRepo = new Mock<IGenericRepository<InternshipStudent>>();
        membershipRepo.Setup(x => x.Query()).Returns(memberships.AsQueryable().BuildMock());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Repository<IOCv2.Domain.Entities.ProjectResources>()).Returns(resourceRepo.Object);
        unitOfWork.Setup(x => x.Repository<Project>()).Returns(projectRepo.Object);
        unitOfWork.Setup(x => x.Repository<Student>()).Returns(studentRepo.Object);
        unitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(membershipRepo.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId.ToString());
        currentUser.Setup(x => x.Role).Returns("Student");

        var message = new Mock<IMessageService>();
        message.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Link download is not supported");

        var handler = new GetDownloadProjectResourceByIdHandler(
            Mock.Of<AutoMapper.IMapper>(),
            unitOfWork.Object,
            Mock.Of<ILogger<GetDownloadProjectResourceByIdHandler>>(),
            message.Object,
            Mock.Of<IFileStorageService>(),
            currentUser.Object);

        var result = await handler.Handle(new GetDownloadProjectResourceByIdQuery { ProjectResourceId = resourceId }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BadRequest);
    }

    [Fact]
    public async Task Handle_ShouldNotUseSpoofedResourceNameExtension_ForDownloadMetadata()
    {
        var resourceId = Guid.NewGuid();
        var internshipId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var project = Project.Create("Demo", string.Empty, "PRJ-DEMO_DMO_2", "IT", "Requirements");
        project.AssignToGroup(internshipId, null, null);
        var projectId = project.ProjectId;

        var resource = new IOCv2.Domain.Entities.ProjectResources(projectId, "avatar.exe", FileType.PNG, "/uploads/normalized.jpg")
        {
            ProjectResourceId = resourceId
        };

        var resourceRepo = new Mock<IGenericRepository<IOCv2.Domain.Entities.ProjectResources>>();
        resourceRepo.Setup(x => x.Query()).Returns(new List<IOCv2.Domain.Entities.ProjectResources> { resource }.AsQueryable().BuildMock());

        var projectRepo = new Mock<IGenericRepository<Project>>();
        projectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());

        var studentRepo = new Mock<IGenericRepository<Student>>();
        studentRepo.Setup(x => x.Query()).Returns(new List<Student> { new() { StudentId = studentId, UserId = userId } }.AsQueryable().BuildMock());

        var membershipRepo = new Mock<IGenericRepository<InternshipStudent>>();
        membershipRepo.Setup(x => x.Query()).Returns(new List<InternshipStudent> { new() { InternshipId = internshipId, StudentId = studentId } }.AsQueryable().BuildMock());

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(x => x.Repository<IOCv2.Domain.Entities.ProjectResources>()).Returns(resourceRepo.Object);
        unitOfWork.Setup(x => x.Repository<Project>()).Returns(projectRepo.Object);
        unitOfWork.Setup(x => x.Repository<Student>()).Returns(studentRepo.Object);
        unitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(membershipRepo.Object);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(userId.ToString());
        currentUser.Setup(x => x.Role).Returns("Student");

        var message = new Mock<IMessageService>();
        message.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("ok");

        var fileStorage = new Mock<IFileStorageService>();
        fileStorage.Setup(x => x.GetFileAsync(resource.ResourceUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        var handler = new GetDownloadProjectResourceByIdHandler(
            Mock.Of<AutoMapper.IMapper>(),
            unitOfWork.Object,
            Mock.Of<ILogger<GetDownloadProjectResourceByIdHandler>>(),
            message.Object,
            fileStorage.Object,
            currentUser.Object);

        var result = await handler.Handle(new GetDownloadProjectResourceByIdQuery { ProjectResourceId = resourceId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.ContentType.Should().Be("image/png");
        result.Data.FileName.Should().Be("avatar.png");
    }
}

using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetProjectRescourceById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

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

        var project = Project.Create(internshipId, "Demo", string.Empty);
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
}

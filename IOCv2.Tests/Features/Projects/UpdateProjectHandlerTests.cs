using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Projects.Commands.UpdateProject;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace IOCv2.Tests.Features.Projects
{
    public class UpdateProjectHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UpdateProjectHandler>> _mockLogger;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepo;
        private readonly Mock<IGenericRepository<InternshipGroup>> _mockInternshipRepo;
        private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
        private readonly Mock<IGenericRepository<InternshipStudent>> _mockInternshipStudentRepo;
        private readonly Mock<IGenericRepository<IOCv2.Domain.Entities.ProjectResources>> _mockProjectResourcesRepo;
        private readonly Mock<INotificationPushService> _mockPushService;
        private readonly Mock<IFileStorageService> _mockFileStorageService;
        private readonly UpdateProjectHandler _handler;

        public UpdateProjectHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UpdateProjectHandler>>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockPushService = new Mock<INotificationPushService>();
            _mockFileStorageService = new Mock<IFileStorageService>();
            
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();
            _mockInternshipRepo = new Mock<IGenericRepository<InternshipGroup>>();
            _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();
            _mockInternshipStudentRepo = new Mock<IGenericRepository<InternshipStudent>>();
            _mockProjectResourcesRepo = new Mock<IGenericRepository<IOCv2.Domain.Entities.ProjectResources>>();

            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockInternshipRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(_mockInternshipStudentRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<IOCv2.Domain.Entities.ProjectResources>()).Returns(_mockProjectResourcesRepo.Object);
            _mockInternshipStudentRepo.Setup(x => x.Query())
                .Returns(new List<InternshipStudent>().AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _handler = new UpdateProjectHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockMessageService.Object,
                _mockCurrentUser.Object,
                _mockCacheService.Object,
                _mockPushService.Object,
                _mockFileStorageService.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var internshipId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var enterpriseUserId = Guid.NewGuid();
            
            var project = Project.Create("Old Name", "Old Description", "PRJ-TEST_TST_1", "IT", "Requirements", mentorId: enterpriseUserId);
            project.Publish();
            project.AssignToGroup(internshipId, null, null);
            var command = new UpdateProjectCommand { ProjectId = project.ProjectId, ProjectName = "Updated Name" };
            
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = enterpriseUserId, UserId = userId }
            }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
            
            _mockMapper.Setup(x => x.Map<UpdateProjectResponse>(It.IsAny<Project>()))
                .Returns(new UpdateProjectResponse { ProjectId = project.ProjectId, ProjectName = "Updated Name" });

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockProjectRepo.Verify(x => x.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ShouldReturnFailure()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var command = new UpdateProjectCommand { ProjectId = projectId, ProjectName = "Updated Name" };
            var userId = Guid.NewGuid();
            var enterpriseUserId = Guid.NewGuid();
            
            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = enterpriseUserId, UserId = userId }
            }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.Query()).Returns(new List<Project>().AsQueryable().BuildMock());
            
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Project not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
            result.Error.Should().Be("Project not found");
        }

        [Fact]
        public async Task Handle_WithResourceUpdatesAndDeletes_ShouldApplyResourceChanges()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseUserId = Guid.NewGuid();
            var project = Project.Create("Project", "Desc", "PRJ-TEST_TST_2", "IT", "Req", mentorId: enterpriseUserId);

            var linkResource = new IOCv2.Domain.Entities.ProjectResources(project.ProjectId, "Original Link", FileType.LINK, "https://old.link")
            {
                ProjectResourceId = Guid.NewGuid()
            };
            var fileResource = new IOCv2.Domain.Entities.ProjectResources(project.ProjectId, "Spec.pdf", FileType.PDF, "/Uploads/spec.pdf")
            {
                ProjectResourceId = Guid.NewGuid()
            };

            project.ProjectResources.Add(linkResource);
            project.ProjectResources.Add(fileResource);

            var command = new UpdateProjectCommand
            {
                ProjectId = project.ProjectId,
                ProjectName = "Updated Project",
                ResourceUpdates = new List<UpdateProjectResourceInput>
                {
                    new()
                    {
                        ProjectResourceId = linkResource.ProjectResourceId,
                        ResourceName = "Updated Link",
                        ExternalUrl = "https://new.link"
                    }
                },
                ResourceDeleteIds = new List<Guid> { fileResource.ProjectResourceId }
            };

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = enterpriseUserId, UserId = userId }
            }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _mockMapper.Setup(x => x.Map<UpdateProjectResponse>(It.IsAny<Project>()))
                .Returns(new UpdateProjectResponse { ProjectId = project.ProjectId, ProjectName = "Updated Project" });

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            linkResource.ResourceName.Should().Be("Updated Link");
            linkResource.ResourceUrl.Should().Be("https://new.link");
            _mockProjectResourcesRepo.Verify(x => x.UpdateAsync(It.IsAny<IOCv2.Domain.Entities.ProjectResources>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockProjectResourcesRepo.Verify(x => x.DeleteAsync(It.IsAny<IOCv2.Domain.Entities.ProjectResources>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithAddedFileAndLink_ShouldCreateResources()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseUserId = Guid.NewGuid();
            var project = Project.Create("Project", "Desc", "PRJ-TEST_TST_3", "IT", "Req", mentorId: enterpriseUserId);

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("dummy"));
            IFormFile formFile = new FormFile(ms, 0, ms.Length, "files", "design.pdf");

            var command = new UpdateProjectCommand
            {
                ProjectId = project.ProjectId,
                ProjectName = "Updated Project",
                Files = new List<IFormFile> { formFile },
                Links = new List<UpdateProjectLinkInput>
                {
                    new() { ResourceName = "Figma", Url = "https://figma.com/file/abc" }
                }
            };

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = enterpriseUserId, UserId = userId }
            }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

            _mockFileStorageService.Setup(x => x.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://cdn.example.com/design.pdf");

            _mockMapper.Setup(x => x.Map<UpdateProjectResponse>(It.IsAny<Project>()))
                .Returns(new UpdateProjectResponse { ProjectId = project.ProjectId, ProjectName = "Updated Project" });

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockFileStorageService.Verify(x => x.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockProjectResourcesRepo.Verify(x => x.AddAsync(It.IsAny<IOCv2.Domain.Entities.ProjectResources>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}

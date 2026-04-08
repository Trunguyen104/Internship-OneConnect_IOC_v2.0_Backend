using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace IOCv2.Tests.Features.Projects
{
    public class CreateProjectHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CreateProjectHandler>> _mockLogger;
        private readonly Mock<IMessageService> _mockMessage;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IGenericRepository<InternshipGroup>> _mockInternshipRepo;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepo;
        private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
        private readonly Mock<IFileStorageService> _mockFileStorage;
        private readonly Mock<INotificationPushService> _mockPushService;
        private readonly CreateProjectHandler _handler;

        public CreateProjectHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CreateProjectHandler>>();
            _mockMessage = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockFileStorage = new Mock<IFileStorageService>();
            _mockPushService = new Mock<INotificationPushService>();

            _mockInternshipRepo = new Mock<IGenericRepository<InternshipGroup>>();
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();
            _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockInternshipRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _handler = new CreateProjectHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockMessage.Object,
                _mockCacheService.Object,
                _mockCurrentUser.Object,
                _mockFileStorage.Object,
                _mockPushService.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseUserId = Guid.NewGuid();
            var command = new CreateProjectCommand
            {
                ProjectName = "New Project",
                Field = "IT",
                Requirements = "Requirements"
            };

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = enterpriseUserId, UserId = userId }
            }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.Query()).Returns(new List<Project>().AsQueryable().BuildMock());

            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper.Setup(x => x.Map<CreateProjectResponse>(It.IsAny<Project>()))
                .Returns(new CreateProjectResponse { ProjectName = "New Project" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockProjectRepo.Verify(
                x => x.AddAsync(
                    It.Is<Project>(p => p.Field == "Software Engineering"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_EnterpriseUserNotFound_ShouldReturnFailure()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var command = new CreateProjectCommand { ProjectName = "New Project", Field = "IT", Requirements = "Requirements" };

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>().AsQueryable().BuildMock());

            _mockMessage.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Mentor not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Mentor not found");
        }

        [Fact]
        public async Task Handle_InvalidUserId_ShouldReturnUnauthorized()
        {
            // Arrange
            var command = new CreateProjectCommand { ProjectName = "New Project" };

            _mockCurrentUser.Setup(x => x.UserId).Returns("not-a-guid");
            _mockMessage.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Unauthorized");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Unauthorized");
        }

        [Fact]
        public async Task Handle_CreateIntoGroupWithExistingActiveProject_ShouldReturnConflict()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mentorId = Guid.NewGuid();
            var group = InternshipGroup.Create(Guid.NewGuid(), "Group 06", mentorId: mentorId);

            var existingActive = Project.Create("Existing", "Desc", "PRJ-EXISTING", "IT", "Req", mentorId: mentorId);
            existingActive.AssignToGroup(group.InternshipId, group.StartDate, group.EndDate);

            var command = new CreateProjectCommand
            {
                ProjectName = "New Project",
                Field = "IT",
                Requirements = "Requirements",
                InternshipGroupId = group.InternshipId
            };

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = mentorId, UserId = userId }
            }.AsQueryable().BuildMock());

            _mockInternshipRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.Query())
                .Returns(new List<Project> { existingActive }.AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
            _mockProjectRepo.Verify(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithGroupAndPublish_ShouldSendNotifications()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var mentorId = Guid.NewGuid();
            var studentUserId = Guid.NewGuid();
            var group = InternshipGroup.Create(Guid.NewGuid(), "Group 06", mentorId: mentorId);
            
            var command = new CreateProjectCommand
            {
                ProjectName = "Notify Project",
                InternshipGroupId = group.InternshipId,
                PublishOnSave = true
            };

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = mentorId, UserId = userId }
            }.AsQueryable().BuildMock());

            _mockInternshipRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());
            
            _mockProjectRepo.Setup(x => x.Query()).Returns(new List<Project>().AsQueryable().BuildMock());

            // Mock InternshipStudent for notifications
            var studentRepo = new Mock<IGenericRepository<InternshipStudent>>();
            studentRepo.Setup(x => x.Query()).Returns(new List<InternshipStudent>
            {
                new InternshipStudent { 
                    InternshipId = group.InternshipId, 
                    Student = new Student { UserId = studentUserId } 
                }
            }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(studentRepo.Object);

            // Mock Notification
            var notificationRepo = new Mock<IGenericRepository<Notification>>();
            _mockUnitOfWork.Setup(x => x.Repository<Notification>()).Returns(notificationRepo.Object);

            _mockMapper.Setup(x => x.Map<CreateProjectResponse>(It.IsAny<Project>()))
                .Returns(new CreateProjectResponse { ProjectName = "Notify Project" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            notificationRepo.Verify(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

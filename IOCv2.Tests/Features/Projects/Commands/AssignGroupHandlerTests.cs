using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Commands.AssignGroup;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace IOCv2.Tests.Features.Projects.Commands
{
    public class AssignGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<ILogger<AssignGroupHandler>> _mockLogger;
        private readonly Mock<INotificationPushService> _mockPushService;

        private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepo;
        private readonly Mock<IGenericRepository<InternshipGroup>> _mockGroupRepo;
        private readonly Mock<IGenericRepository<InternshipStudent>> _mockInternshipStudentRepo;
        private readonly Mock<IGenericRepository<Notification>> _mockNotificationRepo;

        private readonly AssignGroupHandler _handler;

        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _enterpriseId = Guid.NewGuid();
        private readonly Guid _enterpriseUserId = Guid.NewGuid();

        public AssignGroupHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockLogger = new Mock<ILogger<AssignGroupHandler>>();
            _mockPushService = new Mock<INotificationPushService>();

            _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();
            _mockGroupRepo = new Mock<IGenericRepository<InternshipGroup>>();
            _mockInternshipStudentRepo = new Mock<IGenericRepository<InternshipStudent>>();
            _mockNotificationRepo = new Mock<IGenericRepository<Notification>>();

            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockGroupRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(_mockInternshipStudentRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Notification>()).Returns(_mockNotificationRepo.Object);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key);

            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockPushService.Setup(x => x.PushNewNotificationAsync(It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: valid user
            _mockCurrentUser.Setup(x => x.UserId).Returns(_currentUserId.ToString());

            // Default: enterprise user exists
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser
                {
                    UserId = _currentUserId,
                    EnterpriseId = _enterpriseId,
                    EnterpriseUserId = _enterpriseUserId
                }
            }.AsQueryable().BuildMock());

            // Default: no students, no notifications
            _mockInternshipStudentRepo.Setup(x => x.Query())
                .Returns(new List<InternshipStudent>().AsQueryable().BuildMock());
            _mockNotificationRepo.Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Notification n, CancellationToken _) => n);
            _mockNotificationRepo.Setup(x => x.CountAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Default: update returns
            _mockProjectRepo.Setup(x => x.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _handler = new AssignGroupHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUser.Object,
                _mockMessageService.Object,
                _mockCacheService.Object,
                _mockLogger.Object,
                _mockPushService.Object);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUser.Setup(x => x.UserId).Returns("not-a-guid");
            var command = new AssignGroupCommand { ProjectId = Guid.NewGuid(), InternshipId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }

        [Fact]
        public async Task Handle_EnterpriseUserNotFound_ReturnsForbidden()
        {
            // Arrange
            _mockEnterpriseUserRepo.Setup(x => x.Query())
                .Returns(new List<EnterpriseUser>().AsQueryable().BuildMock());

            var command = new AssignGroupCommand { ProjectId = Guid.NewGuid(), InternshipId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockProjectRepo.Setup(x => x.Query())
                .Returns(new List<Project>().AsQueryable().BuildMock());

            var command = new AssignGroupCommand { ProjectId = Guid.NewGuid(), InternshipId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_ProjectAlreadyAssigned_ReturnsBadRequest()
        {
            // Arrange
            // Project with Unstarted status first, then we manually set it to Active via AssignToGroup
            var project = Project.Create(
                "Test Project", "Desc", "PRJ-TEST001", "IT", "Req",
                mentorId: _enterpriseUserId);
            // Assign to a group so OperationalStatus becomes Active
            project.AssignToGroup(Guid.NewGuid(), null, null);

            _mockProjectRepo.Setup(x => x.Query())
                .Returns(new List<Project> { project }.AsQueryable().BuildMock());

            var command = new AssignGroupCommand { ProjectId = project.ProjectId, InternshipId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_GroupNotFound_ReturnsNotFound()
        {
            // Arrange
            var project = Project.Create(
                "Test Project", "Desc", "PRJ-TEST002", "IT", "Req",
                mentorId: _enterpriseUserId);
            // Project is Unstarted (default), MentorId matches enterpriseUserId

            _mockProjectRepo.Setup(x => x.Query())
                .Returns(new List<Project> { project }.AsQueryable().BuildMock());

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());

            var command = new AssignGroupCommand { ProjectId = project.ProjectId, InternshipId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_GroupWithoutMentor_ReturnsBadRequestWithNoMentorMessage()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var project = Project.Create(
                "Test Project", "Desc", "PRJ-TEST004", "IT", "Req",
                mentorId: _enterpriseUserId);

            var group = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "No Mentor Group",
                enterpriseId: _enterpriseId,
                mentorId: null,
                endDate: DateTime.UtcNow.AddDays(30));

            typeof(InternshipGroup)
                .GetProperty("InternshipId")!
                .SetValue(group, groupId);

            _mockProjectRepo.Setup(x => x.Query())
                .Returns(new List<Project> { project }.AsQueryable().BuildMock());

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            var command = new AssignGroupCommand { ProjectId = project.ProjectId, InternshipId = groupId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            result.Error.Should().Be(MessageKeys.Projects.GroupHasNoMentor);
        }

        [Fact]
        public async Task Handle_ValidAssign_ReturnsSuccess()
        {
            // Arrange
            var groupId = Guid.NewGuid();
            var project = Project.Create(
                "Test Project", "Desc", "PRJ-TEST003", "IT", "Req",
                mentorId: _enterpriseUserId);
            // Project is Unstarted by default, MentorId = _enterpriseUserId

            var group = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Test Group",
                enterpriseId: _enterpriseId,
                mentorId: _enterpriseUserId,
                endDate: DateTime.UtcNow.AddDays(30));
            // Set InternshipId via reflection so we can reference it
            typeof(InternshipGroup)
                .GetProperty("InternshipId")!
                .SetValue(group, groupId);

            _mockProjectRepo.Setup(x => x.Query())
                .Returns(new List<Project> { project }.AsQueryable().BuildMock());

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            var command = new AssignGroupCommand { ProjectId = project.ProjectId, InternshipId = groupId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.ProjectId.Should().Be(project.ProjectId);
            result.Data.OperationalStatus.Should().Be(OperationalStatus.Active);
            _mockProjectRepo.Verify(x => x.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

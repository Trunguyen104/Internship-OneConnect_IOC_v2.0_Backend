using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Projects.Commands.DeleteProject;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace IOCv2.Tests.Features.Projects
{
    public class DeleteProjectHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ILogger<DeleteProjectHandler>> _mockLogger;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepo;
        private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
        private readonly Mock<IGenericRepository<WorkItem>> _mockWorkItemRepo;
        private readonly Mock<IGenericRepository<Sprint>> _mockSprintRepo;
        private readonly Mock<IGenericRepository<Logbook>> _mockLogbookRepo;
        private readonly Mock<IGenericRepository<Stakeholder>> _mockStakeholderRepo;
        private readonly Mock<IGenericRepository<Evaluation>> _mockEvaluationRepo;
        private readonly Mock<IGenericRepository<ViolationReport>> _mockViolationRepo;
        private readonly Mock<INotificationPushService> _mockPushService;
        private readonly DeleteProjectHandler _handler;

        public DeleteProjectHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<DeleteProjectHandler>>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockPushService = new Mock<INotificationPushService>();
            
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();
            _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();
            _mockWorkItemRepo = new Mock<IGenericRepository<WorkItem>>();
            _mockSprintRepo = new Mock<IGenericRepository<Sprint>>();
            _mockLogbookRepo = new Mock<IGenericRepository<Logbook>>();
            _mockStakeholderRepo = new Mock<IGenericRepository<Stakeholder>>();
            _mockEvaluationRepo = new Mock<IGenericRepository<Evaluation>>();
            _mockViolationRepo = new Mock<IGenericRepository<ViolationReport>>();

            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<WorkItem>()).Returns(_mockWorkItemRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Sprint>()).Returns(_mockSprintRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Logbook>()).Returns(_mockLogbookRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>()).Returns(_mockStakeholderRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Evaluation>()).Returns(_mockEvaluationRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<ViolationReport>()).Returns(_mockViolationRepo.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _handler = new DeleteProjectHandler(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMessageService.Object,
                _mockCurrentUser.Object,
                _mockCacheService.Object,
                _mockPushService.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var enterpriseUserId = Guid.NewGuid();
            var project = Project.Create("Project for Deletion", "Description", "PRJ-TEST_TST_1", "IT", "Requirements", mentorId: enterpriseUserId);
            var command = new DeleteProjectCommand { ProjectId = project.ProjectId };

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = enterpriseUserId, UserId = userId }
            }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());
            _mockWorkItemRepo.Setup(x => x.Query()).Returns(new List<WorkItem>().AsQueryable().BuildMock());
            _mockSprintRepo.Setup(x => x.Query()).Returns(new List<Sprint>().AsQueryable().BuildMock());
            _mockLogbookRepo.Setup(x => x.Query()).Returns(new List<Logbook>().AsQueryable().BuildMock());
            _mockStakeholderRepo.Setup(x => x.Query()).Returns(new List<Stakeholder>().AsQueryable().BuildMock());
            _mockEvaluationRepo.Setup(x => x.Query()).Returns(new List<Evaluation>().AsQueryable().BuildMock());
            _mockViolationRepo.Setup(x => x.Query()).Returns(new List<ViolationReport>().AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Delete successful");
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockProjectRepo.Verify(x => x.HardDeleteAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ShouldReturnFailure()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var command = new DeleteProjectCommand { ProjectId = projectId };
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
        public async Task Handle_InternshipHasLogbookData_ShouldBlockDelete()
        {
            var userId = Guid.NewGuid();
            var enterpriseUserId = Guid.NewGuid();
            var group = InternshipGroup.Create(Guid.NewGuid(), "IG", mentorId: enterpriseUserId);
            var project = Project.Create("Project for Deletion", "Description", "PRJ-TEST_TST_2", "Software Engineering", "Requirements", mentorId: enterpriseUserId);
            project.AssignToGroup(group.InternshipId, DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(30));
            project.InternshipGroup = group;

            _mockCurrentUser.Setup(x => x.UserId).Returns(userId.ToString());
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser { EnterpriseUserId = enterpriseUserId, UserId = userId }
            }.AsQueryable().BuildMock());
            _mockProjectRepo.Setup(x => x.Query()).Returns(new List<Project> { project }.AsQueryable().BuildMock());
            _mockWorkItemRepo.Setup(x => x.Query()).Returns(new List<WorkItem>().AsQueryable().BuildMock());
            _mockSprintRepo.Setup(x => x.Query()).Returns(new List<Sprint>().AsQueryable().BuildMock());
            _mockLogbookRepo.Setup(x => x.Query()).Returns(new List<Logbook>
            {
                Logbook.Create(group.InternshipId, Guid.NewGuid(), "sum", null, "plan", DateTime.UtcNow.Date)
            }.AsQueryable().BuildMock());

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Has data");

            var result = await _handler.Handle(new DeleteProjectCommand { ProjectId = project.ProjectId }, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockProjectRepo.Verify(x => x.HardDeleteAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

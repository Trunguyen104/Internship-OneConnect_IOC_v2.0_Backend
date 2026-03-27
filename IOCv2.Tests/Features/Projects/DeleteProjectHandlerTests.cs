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
        private readonly DeleteProjectHandler _handler;

        public DeleteProjectHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<DeleteProjectHandler>>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();
            
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();
            _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();
            _mockWorkItemRepo = new Mock<IGenericRepository<WorkItem>>();
            _mockSprintRepo = new Mock<IGenericRepository<Sprint>>();

            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<WorkItem>()).Returns(_mockWorkItemRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Sprint>()).Returns(_mockSprintRepo.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _handler = new DeleteProjectHandler(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMessageService.Object,
                _mockCurrentUser.Object,
                _mockCacheService.Object);
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
    }
}

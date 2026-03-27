using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Projects.Commands.UpdateProject;
using IOCv2.Domain.Entities;
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
        private readonly UpdateProjectHandler _handler;

        public UpdateProjectHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UpdateProjectHandler>>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();
            
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();
            _mockInternshipRepo = new Mock<IGenericRepository<InternshipGroup>>();
            _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();
            _mockInternshipStudentRepo = new Mock<IGenericRepository<InternshipStudent>>();

            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockInternshipRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(_mockInternshipStudentRepo.Object);
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
                _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var internshipId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var enterpriseUserId = Guid.NewGuid();
            
            var project = Project.Create("Old Name", "Old Description", "PRJ-TEST_TST_1", "IT", "Requirements", mentorId: enterpriseUserId);
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
    }
}

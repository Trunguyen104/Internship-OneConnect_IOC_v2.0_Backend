using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Projects.Commands.DeleteProject;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
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
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepo;
        private readonly DeleteProjectHandler _handler;

        public DeleteProjectHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockLogger = new Mock<ILogger<DeleteProjectHandler>>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUser = new Mock<ICurrentUserService>();
            
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();

            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);

            _handler = new DeleteProjectHandler(
                _mockUnitOfWork.Object,
                _mockLogger.Object,
                _mockMessageService.Object,
                _mockCurrentUser.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var command = new DeleteProjectCommand { ProjectId = projectId };
            
            var project = Project.Create(Guid.NewGuid(), "Project for Deletion", "Description");
            
            _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
            _mockProjectRepo.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(project);
            
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Delete successful");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            project.DeletedAt.Should().NotBeNull();
            _mockProjectRepo.Verify(x => x.UpdateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ProjectNotFound_ShouldReturnFailure()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var command = new DeleteProjectCommand { ProjectId = projectId };
            
            _mockCurrentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
            _mockProjectRepo.Setup(x => x.GetByIdAsync(projectId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Project?)null);
            
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

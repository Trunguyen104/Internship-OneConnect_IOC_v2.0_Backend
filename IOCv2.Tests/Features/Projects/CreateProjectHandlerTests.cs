using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
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
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IGenericRepository<InternshipGroup>> _mockInternshipRepo;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepo;
        private readonly CreateProjectHandler _handler;

        public CreateProjectHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CreateProjectHandler>>();
            _mockMessage = new Mock<IMessageService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            
            _mockInternshipRepo = new Mock<IGenericRepository<InternshipGroup>>();
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockInternshipRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);

            _handler = new CreateProjectHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object,
                _mockMessage.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var internshipId = Guid.NewGuid();
            var command = new CreateProjectCommand { ProjectName = "New Project", InternshipId = internshipId };

            _mockInternshipRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            _mockProjectRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mockMapper.Setup(x => x.Map<CreateProjectResponse>(It.IsAny<Project>()))
                .Returns(new CreateProjectResponse { ProjectName = "New Project" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockProjectRepo.Verify(x => x.AddAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_InternshipNotFound_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateProjectCommand { ProjectName = "New Project", InternshipId = Guid.NewGuid() };

            _mockInternshipRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            
            _mockMessage.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Internship not found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Internship not found");
        }

        [Fact]
        public async Task Handle_ProjectAlreadyExists_ShouldReturnFailure()
        {
            // Arrange
            var internshipId = Guid.NewGuid();
            var command = new CreateProjectCommand { ProjectName = "New Project", InternshipId = internshipId };

            _mockInternshipRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            _mockProjectRepo.Setup(x => x.ExistsAsync(It.IsAny<Expression<Func<Project, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            _mockMessage.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Project already exists");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project already exists");
        }
    }
}

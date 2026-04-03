using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.StakeholderIssues.Commands
{
    public class CreateStakeholderIssueHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<CreateStakeholderIssueCommandHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateStakeholderIssueCommandHandler _handler;

        private readonly Guid _stakeholderId = Guid.NewGuid();
        private readonly Guid _internshipId = Guid.NewGuid();
        private readonly Guid _currentUserId = Guid.NewGuid();

        private static Stakeholder CreateStakeholder(Guid stakeholderId)
        {
            var stakeholder = new Stakeholder(
                Guid.NewGuid(),
                "Test Stakeholder",
                IOCv2.Domain.Enums.StakeholderType.Real,
                "test@example.com",
                null, null, null);
            typeof(Stakeholder).GetProperty("Id")!.SetValue(stakeholder, stakeholderId);
            return stakeholder;
        }

        public CreateStakeholderIssueHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<CreateStakeholderIssueCommandHandler>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns((string key) => key);

            _mockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId.ToString());
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: Stakeholder exists with correct Id
            var stakeholder = new Stakeholder(
                _internshipId,
                "Test Stakeholder",
                IOCv2.Domain.Enums.StakeholderType.Real,
                "test@example.com",
                null,
                null,
                null);
            typeof(Stakeholder).GetProperty("Id")!.SetValue(stakeholder, _stakeholderId);
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().Query())
                .Returns(new List<Stakeholder> { stakeholder }.AsQueryable().BuildMock());

            var internshipGroup = InternshipGroup.Create(Guid.NewGuid(), "Group A");
            typeof(InternshipGroup).GetProperty("InternshipId")!.SetValue(internshipGroup, _internshipId);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup> { internshipGroup }.AsQueryable().BuildMock());

            // Default: AddAsync returns the entity
            _mockUnitOfWork.Setup(x => x.Repository<StakeholderIssue>().AddAsync(It.IsAny<StakeholderIssue>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((StakeholderIssue e, CancellationToken _) => e);

            // Default: mapper returns response
            _mockMapper.Setup(x => x.Map<CreateStakeholderIssueResponse>(It.IsAny<StakeholderIssue>()))
                .Returns(new CreateStakeholderIssueResponse { Title = "Test Issue" });

            _handler = new CreateStakeholderIssueCommandHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object,
                _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_StakeholderNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().Query())
                .Returns(new List<Stakeholder>().AsQueryable().BuildMock());

            var command = new CreateStakeholderIssueCommand
            {
                StakeholderId = Guid.NewGuid(),
                Title = "Issue Title",
                Description = "Issue Description"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var command = new CreateStakeholderIssueCommand
            {
                StakeholderId = _stakeholderId,
                Title = "Issue Title",
                Description = "Issue Description"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.Repository<StakeholderIssue>().AddAsync(It.IsAny<StakeholderIssue>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_MentorRole_ReturnsForbidden()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Role).Returns("Mentor");

            var command = new CreateStakeholderIssueCommand
            {
                StakeholderId = _stakeholderId,
                Title = "Issue Title",
                Description = "Issue Description"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }
    }
}

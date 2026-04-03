using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
    public class DeleteStakeholderIssueHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<DeleteStakeholderIssueCommandHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly DeleteStakeholderIssueCommandHandler _handler;

        private readonly Guid _issueId = Guid.NewGuid();
        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _stakeholderId = Guid.NewGuid();
        private readonly Guid _internshipId = Guid.NewGuid();

        private static StakeholderIssue CreateIssue(Guid issueId)
        {
            return new StakeholderIssue(
                issueId,
                Guid.NewGuid(),
                "Test Issue",
                "Test Description");
        }

        public DeleteStakeholderIssueHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<DeleteStakeholderIssueCommandHandler>>();
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

            // Default: issue exists
            var issue = new StakeholderIssue(_issueId, _stakeholderId, "Test Issue", "Test Description");
            _mockUnitOfWork.Setup(x => x.Repository<StakeholderIssue>().Query())
                .Returns(new List<StakeholderIssue> { issue }.AsQueryable().BuildMock());

            var stakeholder = new Stakeholder(
                _internshipId,
                "Test Stakeholder",
                StakeholderType.Real,
                "stakeholder@example.com",
                null,
                null,
                null);
            typeof(Stakeholder).GetProperty("Id")!.SetValue(stakeholder, _stakeholderId);
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().Query())
                .Returns(new List<Stakeholder> { stakeholder }.AsQueryable().BuildMock());

            // Default: HardDeleteAsync returns Task.CompletedTask
            _mockUnitOfWork.Setup(x => x.Repository<StakeholderIssue>().HardDeleteAsync(It.IsAny<StakeholderIssue>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: mapper returns response
            _mockMapper.Setup(x => x.Map<DeleteStakeholderIssueResponse>(It.IsAny<StakeholderIssue>()))
                .Returns(new DeleteStakeholderIssueResponse { Id = _issueId });

            _handler = new DeleteStakeholderIssueCommandHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object,
                _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_IssueNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(x => x.Repository<StakeholderIssue>().Query())
                .Returns(new List<StakeholderIssue>().AsQueryable().BuildMock());

            var command = new DeleteStakeholderIssueCommand { Id = Guid.NewGuid() };

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
            var command = new DeleteStakeholderIssueCommand { Id = _issueId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.Repository<StakeholderIssue>().HardDeleteAsync(It.IsAny<StakeholderIssue>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_MentorRole_ReturnsForbidden()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Role).Returns("Mentor");
            var command = new DeleteStakeholderIssueCommand { Id = _issueId };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }
    }
}

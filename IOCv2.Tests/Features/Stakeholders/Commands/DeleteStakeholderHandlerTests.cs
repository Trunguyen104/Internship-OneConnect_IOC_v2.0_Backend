using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder;
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

namespace IOCv2.Tests.Features.Stakeholders.Commands
{
    public class DeleteStakeholderHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<DeleteStakeholderHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly DeleteStakeholderHandler _handler;

        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _internshipId = Guid.NewGuid();
        private readonly Guid _stakeholderId = Guid.NewGuid();

        /// <summary>
        /// Creates a Stakeholder with a specific Id set via reflection (Id has private setter).
        /// </summary>
        private Stakeholder CreateStakeholder(Guid internshipId)
        {
            var stakeholder = new Stakeholder(
                internshipId,
                "Test Stakeholder",
                StakeholderType.Real,
                "stakeholder@example.com",
                "Tester",
                "Test description",
                "0900000000");
            typeof(Stakeholder).GetProperty("Id")!.SetValue(stakeholder, _stakeholderId);
            return stakeholder;
        }

        public DeleteStakeholderHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<DeleteStakeholderHandler>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();

            // Default: valid user, SuperAdmin role (skip auth check)
            _mockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId.ToString());
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            // Default: stakeholder exists
            var stakeholder = CreateStakeholder(_internshipId);
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().Query())
                .Returns(new List<Stakeholder> { stakeholder }.AsQueryable().BuildMock());

            // Default: InternshipGroup auth query not needed for admin
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());

            // Default: message service returns key as value
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns((string key) => key);

            // Default: transaction and save mocks
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Default: DeleteAsync (soft delete) returns Task
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().DeleteAsync(It.IsAny<Stakeholder>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: cache invalidation
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: mapper returns response
            _mockMapper.Setup(x => x.Map<DeleteStakeholderResponse>(It.IsAny<Stakeholder>()))
                .Returns(new DeleteStakeholderResponse());

            _handler = new DeleteStakeholderHandler(
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
            // Arrange: empty stakeholder repository
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().Query())
                .Returns(new List<Stakeholder>().AsQueryable().BuildMock());

            var command = new DeleteStakeholderCommand
            {
                StakeholderId = Guid.NewGuid(),
                InternshipId = _internshipId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.UserId).Returns("not-a-valid-guid");

            var command = new DeleteStakeholderCommand
            {
                StakeholderId = _stakeholderId,
                InternshipId = _internshipId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }

        [Fact]
        public async Task Handle_MentorRole_ReturnsForbidden()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Role).Returns("Mentor");

            var command = new DeleteStakeholderCommand
            {
                StakeholderId = _stakeholderId,
                InternshipId = _internshipId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_NotAuthorized_ReturnsForbidden()
        {
            // Arrange: regular Student role, not a member/mentor of the group
            _mockCurrentUserService.Setup(x => x.Role).Returns("Student");

            // InternshipGroup auth check returns empty → not authorized
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());

            var command = new DeleteStakeholderCommand
            {
                StakeholderId = _stakeholderId,
                InternshipId = _internshipId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_ValidDelete_AsAdmin_ReturnsSuccess()
        {
            // Arrange: SuperAdmin, valid stakeholder exists
            var command = new DeleteStakeholderCommand
            {
                StakeholderId = _stakeholderId,
                InternshipId = _internshipId
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.Repository<Stakeholder>().DeleteAsync(It.IsAny<Stakeholder>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

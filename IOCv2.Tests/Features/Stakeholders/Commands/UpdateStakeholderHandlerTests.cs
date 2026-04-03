using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder;
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
    public class UpdateStakeholderHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<UpdateStakeholderHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly UpdateStakeholderHandler _handler;

        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _internshipId = Guid.NewGuid();
        private readonly Guid _stakeholderId = Guid.NewGuid();

        /// <summary>
        /// Creates a Stakeholder with a specific Id set via reflection (Id has private setter).
        /// </summary>
        private Stakeholder CreateStakeholder(Guid internshipId, string email = "old@example.com")
        {
            var stakeholder = new Stakeholder(
                internshipId,
                "Old Name",
                StakeholderType.Real,
                email,
                "Old Role",
                "Old Description",
                "0900000000");
            typeof(Stakeholder).GetProperty("Id")!.SetValue(stakeholder, _stakeholderId);
            return stakeholder;
        }

        public UpdateStakeholderHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<UpdateStakeholderHandler>>();
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

            // Default: UpdateAsync returns Task
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().UpdateAsync(It.IsAny<Stakeholder>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: cache invalidation
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: mapper returns response
            _mockMapper.Setup(x => x.Map<UpdateStakeholderResponse>(It.IsAny<Stakeholder>()))
                .Returns(new UpdateStakeholderResponse { Name = "Updated Name" });

            _handler = new UpdateStakeholderHandler(
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

            var command = new UpdateStakeholderCommand
            {
                StakeholderId = Guid.NewGuid(),
                InternshipId = _internshipId,
                Name = "New Name"
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

            var command = new UpdateStakeholderCommand
            {
                StakeholderId = _stakeholderId,
                InternshipId = _internshipId,
                Name = "New Name"
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

            var command = new UpdateStakeholderCommand
            {
                StakeholderId = _stakeholderId,
                InternshipId = _internshipId,
                Name = "New Name"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_EmailDuplicate_ReturnsConflict()
        {
            // Arrange: another stakeholder already has the new email in the same internship
            var anotherStakeholderId = Guid.NewGuid();
            var newEmail = "taken@example.com";

            var targetStakeholder = CreateStakeholder(_internshipId, "old@example.com");

            var conflictingStakeholder = new Stakeholder(
                _internshipId,
                "Another Stakeholder",
                StakeholderType.Real,
                newEmail,
                null,
                null,
                null);
            typeof(Stakeholder).GetProperty("Id")!.SetValue(conflictingStakeholder, anotherStakeholderId);

            // First Query() → returns the target stakeholder (find by id+internshipId)
            // Second Query() → returns the conflicting stakeholder (email duplicate check)
            _mockUnitOfWork.SetupSequence(x => x.Repository<Stakeholder>().Query())
                .Returns(new List<Stakeholder> { targetStakeholder }.AsQueryable().BuildMock())
                .Returns(new List<Stakeholder> { conflictingStakeholder }.AsQueryable().BuildMock());

            var command = new UpdateStakeholderCommand
            {
                StakeholderId = _stakeholderId,
                InternshipId = _internshipId,
                Email = newEmail
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_ValidUpdate_AsAdmin_ReturnsSuccess()
        {
            // Arrange: SuperAdmin, valid stakeholder exists, no email change
            var command = new UpdateStakeholderCommand
            {
                StakeholderId = _stakeholderId,
                InternshipId = _internshipId,
                Name = "Updated Name",
                Type = StakeholderType.Persona,
                Role = "Updated Role",
                Description = "Updated Description",
                PhoneNumber = "0911111111"
                // No Email change → skips duplicate check
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.Repository<Stakeholder>().UpdateAsync(It.IsAny<Stakeholder>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

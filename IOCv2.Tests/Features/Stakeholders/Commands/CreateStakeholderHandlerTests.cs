using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder;
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
    public class CreateStakeholderHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<CreateStakeholderHandler>> _mockLogger;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateStakeholderHandler _handler;

        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _internshipId;

        public CreateStakeholderHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<CreateStakeholderHandler>>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCacheService = new Mock<ICacheService>();

            // Default: valid user, SuperAdmin role (skip auth check)
            _mockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId.ToString());
            _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");

            // Default: InternshipGroup exists
            var defaultGroup = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Group A");
            _internshipId = defaultGroup.InternshipId;

            var internshipGroups = new List<InternshipGroup> { defaultGroup };
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(internshipGroups.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Default: no stakeholder with same email
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().Query())
                .Returns(new List<Stakeholder>().AsQueryable().BuildMock());

            // Default: message service returns key as value
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns((string key) => key);

            // Default: transaction and save mocks
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Default: AddAsync returns the entity
            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().AddAsync(It.IsAny<Stakeholder>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Stakeholder s, CancellationToken c) => s);

            // Default: cache invalidation
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: mapper returns response
            _mockMapper.Setup(x => x.Map<CreateStakeholderResponse>(It.IsAny<Stakeholder>()))
                .Returns(new CreateStakeholderResponse { Name = "Test Stakeholder" });

            _handler = new CreateStakeholderHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCurrentUserService.Object,
                _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_InternshipNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var command = new CreateStakeholderCommand
            {
                InternshipId = Guid.NewGuid(),
                Name = "Test Stakeholder",
                Email = "test@example.com",
                Type = StakeholderType.Real
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

            var command = new CreateStakeholderCommand
            {
                InternshipId = _internshipId,
                Name = "Test Stakeholder",
                Email = "test@example.com",
                Type = StakeholderType.Real
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }

        [Fact]
        public async Task Handle_NotAuthorized_ReturnsForbidden()
        {
            // Arrange: user is not admin and not a member/mentor of the group
            _mockCurrentUserService.Setup(x => x.Role).Returns("Student");

            var authorizedGroup = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Group A");

            // Second call to Repository<InternshipGroup>().Query() for auth check -> not authorized
            _mockUnitOfWork.SetupSequence(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup> { authorizedGroup }.AsQueryable().BuildMock())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock()); // auth check returns empty

            var command = new CreateStakeholderCommand
            {
                InternshipId = authorizedGroup.InternshipId,
                Name = "Test Stakeholder",
                Email = "test@example.com",
                Type = StakeholderType.Real
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_MentorRole_ReturnsForbidden()
        {
            // Arrange: mentor is explicitly blocked from create action
            _mockCurrentUserService.Setup(x => x.Role).Returns("Mentor");

            var command = new CreateStakeholderCommand
            {
                InternshipId = _internshipId,
                Name = "Test Stakeholder",
                Email = "mentor-blocked@example.com",
                Type = StakeholderType.Real
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_EmailAlreadyExists_ReturnsConflict()
        {
            // Arrange: existing stakeholder with same email in same internship
            var duplicateEmail = "duplicate@example.com";
            var existingStakeholder = new Stakeholder(
                _internshipId,
                "Existing Stakeholder",
                StakeholderType.Real,
                duplicateEmail,
                null,
                null,
                null);

            _mockUnitOfWork.Setup(x => x.Repository<Stakeholder>().Query())
                .Returns(new List<Stakeholder> { existingStakeholder }.AsQueryable().BuildMock());

            var command = new CreateStakeholderCommand
            {
                InternshipId = _internshipId,
                Name = "New Stakeholder",
                Email = duplicateEmail,
                Type = StakeholderType.Real
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Conflict);
        }

        [Fact]
        public async Task Handle_ValidRequest_AsAdmin_ReturnsSuccess()
        {
            // Arrange: SuperAdmin role (default), valid internship, no email duplicate
            var command = new CreateStakeholderCommand
            {
                InternshipId = _internshipId,
                Name = "New Stakeholder",
                Email = "new@example.com",
                Type = StakeholderType.Real,
                Role = "Product Owner",
                Description = "Key stakeholder",
                PhoneNumber = "0901234567"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.Repository<Stakeholder>().AddAsync(It.IsAny<Stakeholder>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

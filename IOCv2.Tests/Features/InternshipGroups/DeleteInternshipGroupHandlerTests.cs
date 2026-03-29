using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MockQueryable.Moq;
using MockQueryable;

namespace IOCv2.Tests.Features.InternshipGroups
{
    public class DeleteInternshipGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<DeleteInternshipGroupHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<INotificationPushService> _mockPushService;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepository;
        private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
        private readonly Mock<IGenericRepository<InternshipGroup>> _mockGroupRepo;
        private readonly DeleteInternshipGroupHandler _handler;

        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _enterpriseId = Guid.NewGuid();

        public DeleteInternshipGroupHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<DeleteInternshipGroupHandler>>();
            _mockCacheService = new Mock<ICacheService>();
            _mockPushService = new Mock<INotificationPushService>();
            _mockProjectRepository = new Mock<IGenericRepository<Project>>();
            _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();
            _mockGroupRepo = new Mock<IGenericRepository<InternshipGroup>>();

            // Wire up repositories via explicit mocks (avoid chained setup issues)
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepository.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockGroupRepo.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Default: valid user
            _mockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId.ToString());

            // Default: enterprise user in same enterprise
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser
                {
                    EnterpriseUserId = Guid.NewGuid(),
                    UserId = _currentUserId,
                    EnterpriseId = _enterpriseId
                }
            }.AsQueryable().BuildMock());

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns(string.Empty);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _handler = new DeleteInternshipGroupHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockCacheService.Object,
                _mockPushService.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var existingGroup = InternshipGroup.Create(Guid.NewGuid(), "Test Group", enterpriseId: _enterpriseId);
            var internshipId = existingGroup.InternshipId;
            var command = new DeleteInternshipGroupCommand { InternshipId = internshipId };

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { existingGroup }.AsQueryable().BuildMock());
            _mockProjectRepository.Setup(x => x.Query())
                .Returns(new List<Project>().AsQueryable().BuildMock());

            _mockMapper.Setup(x => x.Map<DeleteInternshipGroupResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new DeleteInternshipGroupResponse { InternshipId = internshipId });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockGroupRepo.Verify(x => x.DeleteAsync(existingGroup, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NotFound_ShouldReturnNotFound()
        {
            // Arrange
            var command = new DeleteInternshipGroupCommand { InternshipId = Guid.NewGuid() };

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());

            _mockMessageService.Setup(x => x.GetMessage(MessageKeys.Common.NotFound))
                .Returns("Not Found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_DifferentEnterprise_ShouldReturnForbidden()
        {
            // Arrange
            var otherEnterpriseId = Guid.NewGuid();
            var existingGroup = InternshipGroup.Create(Guid.NewGuid(), "Other Enterprise Group", enterpriseId: otherEnterpriseId);
            var command = new DeleteInternshipGroupCommand { InternshipId = existingGroup.InternshipId };

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { existingGroup }.AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_ArchivedGroup_ShouldReturnBadRequest()
        {
            // Arrange
            var existingGroup = InternshipGroup.Create(Guid.NewGuid(), "Archived Group", enterpriseId: _enterpriseId);
            existingGroup.UpdateStatus(GroupStatus.Archived);
            var command = new DeleteInternshipGroupCommand { InternshipId = existingGroup.InternshipId };

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { existingGroup }.AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }
    }
}

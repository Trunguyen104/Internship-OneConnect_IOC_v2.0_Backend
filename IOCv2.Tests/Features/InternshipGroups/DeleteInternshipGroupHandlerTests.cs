using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
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
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<DeleteInternshipGroupHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<INotificationPushService> _mockPushService;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepository;
        private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepository;
        private readonly DeleteInternshipGroupHandler _handler;

        public DeleteInternshipGroupHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<DeleteInternshipGroupHandler>>();
            _mockCacheService = new Mock<ICacheService>();
            _mockPushService = new Mock<INotificationPushService>();
            _mockProjectRepository = new Mock<IGenericRepository<Project>>();
            _mockEnterpriseUserRepository = new Mock<IGenericRepository<EnterpriseUser>>();

            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepository.Object);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepository.Object);
            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

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
            var currentUserId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var existingGroup = InternshipGroup.Create(Guid.NewGuid(), "Test Group", enterpriseId: enterpriseId);
            var internshipId = existingGroup.InternshipId;
            var command = new DeleteInternshipGroupCommand { InternshipId = internshipId };

            _mockCurrentUserService.Setup(x => x.UserId).Returns(currentUserId.ToString());
            _mockEnterpriseUserRepository.Setup(x => x.Query())
                .Returns(new List<EnterpriseUser>
                {
                    new EnterpriseUser
                    {
                        EnterpriseUserId = Guid.NewGuid(),
                        UserId = currentUserId,
                        EnterpriseId = enterpriseId
                    }
                }.AsQueryable().BuildMock());
            
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup> { existingGroup }.AsQueryable().BuildMock());
            _mockProjectRepository.Setup(x => x.Query())
                .Returns(new List<Project>().AsQueryable().BuildMock());
            
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            _mockMapper.Setup(x => x.Map<DeleteInternshipGroupResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new DeleteInternshipGroupResponse { InternshipId = internshipId });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.Repository<InternshipGroup>().DeleteAsync(existingGroup, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NotFound_ShouldReturnNotFound()
        {
            // Arrange
            var currentUserId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var command = new DeleteInternshipGroupCommand { InternshipId = Guid.NewGuid() };

            _mockCurrentUserService.Setup(x => x.UserId).Returns(currentUserId.ToString());
            _mockEnterpriseUserRepository.Setup(x => x.Query())
                .Returns(new List<EnterpriseUser>
                {
                    new EnterpriseUser
                    {
                        EnterpriseUserId = Guid.NewGuid(),
                        UserId = currentUserId,
                        EnterpriseId = enterpriseId
                    }
                }.AsQueryable().BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());
            _mockProjectRepository.Setup(x => x.Query())
                .Returns(new List<Project>().AsQueryable().BuildMock());
            
            _mockMessageService.Setup(x => x.GetMessage(MessageKeys.Common.NotFound))
                .Returns("Not Found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}

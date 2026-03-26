using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup;
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
    public class RemoveStudentsFromGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<RemoveStudentsFromGroupHandler>> _mockLogger;
        private readonly Mock<IGenericRepository<InternshipGroup>> _mockGroupRepository;
        private readonly Mock<IGenericRepository<InternshipStudent>> _mockInternshipStudentRepository;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<INotificationPushService> _mockPushService;
        private readonly RemoveStudentsFromGroupHandler _handler;

        public RemoveStudentsFromGroupHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<RemoveStudentsFromGroupHandler>>();
            _mockGroupRepository = new Mock<IGenericRepository<InternshipGroup>>();
            _mockInternshipStudentRepository = new Mock<IGenericRepository<InternshipStudent>>();
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockGroupRepository.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(_mockInternshipStudentRepository.Object);
            _mockCacheService = new Mock<ICacheService>();
            _mockPushService = new Mock<INotificationPushService>();

            _handler = new RemoveStudentsFromGroupHandler(
                _mockUnitOfWork.Object,
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
            var studentId = Guid.NewGuid();
            var group = InternshipGroup.Create(Guid.NewGuid(), "Group Name");
            var internshipId = group.InternshipId;
            
            var command = new RemoveStudentsFromGroupCommand
            {
                InternshipId = internshipId,
                StudentIds = new List<Guid> { studentId }
            };

            group.AddMember(studentId, IOCv2.Domain.Enums.InternshipRole.Member);
            
            _mockGroupRepository.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());
            
            _mockInternshipStudentRepository
                .Setup(x => x.HardDeleteAsync(It.IsAny<InternshipStudent>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            _mockMapper.Setup(x => x.Map<RemoveStudentsFromGroupResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new RemoveStudentsFromGroupResponse { InternshipId = internshipId });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            // Verify DeleteAsync was called once for the student member
            _mockInternshipStudentRepository.Verify(
                x => x.HardDeleteAsync(It.Is<InternshipStudent>(m => m.StudentId == studentId), It.IsAny<CancellationToken>()),
                Times.Once);
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NotFound_ShouldReturnNotFound()
        {
            // Arrange
            var command = new RemoveStudentsFromGroupCommand { InternshipId = Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());
            
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

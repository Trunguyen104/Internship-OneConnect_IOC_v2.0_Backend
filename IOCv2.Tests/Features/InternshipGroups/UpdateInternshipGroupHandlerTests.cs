using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using MockQueryable.Moq;
using MockQueryable;

namespace IOCv2.Tests.Features.InternshipGroups
{
    public class UpdateInternshipGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UpdateInternshipGroupHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<INotificationPushService> _mockPushService;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly UpdateInternshipGroupHandler _handler;

        public UpdateInternshipGroupHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageService = new Mock<IMessageService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UpdateInternshipGroupHandler>>();
            _mockCacheService = new Mock<ICacheService>();
            _mockPushService = new Mock<INotificationPushService>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid().ToString());

            _handler = new UpdateInternshipGroupHandler(
                _mockUnitOfWork.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockCacheService.Object,
                _mockPushService.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_ValidRequest_ShouldReturnSuccess()
        {
            // Arrange
            var phaseId = Guid.NewGuid();
            var existingGroup = InternshipGroup.Create(phaseId, "Old Name");
            var internshipId = existingGroup.InternshipId;

            var command = new UpdateInternshipGroupCommand
            {
                InternshipId = internshipId,
                PhaseId = phaseId,
                GroupName = "Updated Name"
            };
            // Set private InternshipId via reflection if needed, but here we can just ensure the mock returns it

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup> { existingGroup }.BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>().ExistsAsync(It.IsAny<Expression<Func<InternshipPhase, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            
            _mockUnitOfWork.Setup(x => x.Repository<Enterprise>().ExistsAsync(It.IsAny<Expression<Func<Enterprise, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            _mockMapper.Setup(x => x.Map<UpdateInternshipGroupResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new UpdateInternshipGroupResponse { InternshipId = internshipId, GroupName = "Updated Name" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            existingGroup.GroupName.Should().Be("Updated Name");
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NotFound_ShouldReturnNotFound()
        {
            // Arrange
            var command = new UpdateInternshipGroupCommand { InternshipId = Guid.NewGuid() };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup>().BuildMock());
            
            _mockMessageService.Setup(x => x.GetMessage(MessageKeys.Common.NotFound))
                .Returns("Not Found");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_WhenMentorChanged_ShouldSyncProjectMentorOwnership()
        {
            // Arrange
            var phaseId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var oldMentorEnterpriseUserId = Guid.NewGuid();
            var newMentorUserId = Guid.NewGuid();
            var newMentorEnterpriseUserId = Guid.NewGuid();

            var existingGroup = InternshipGroup.Create(
                phaseId,
                "Old Name",
                mentorId: oldMentorEnterpriseUserId);

            var internshipId = existingGroup.InternshipId;
            var project = Project.Create(
                "Project A",
                "Desc",
                "PRJ-A",
                "IT",
                "Req",
                mentorId: oldMentorEnterpriseUserId);
            project.AssignToGroup(internshipId, DateTime.UtcNow.Date.AddDays(-3), DateTime.UtcNow.Date.AddDays(7));

            var command = new UpdateInternshipGroupCommand
            {
                InternshipId = internshipId,
                PhaseId = phaseId,
                GroupName = "Updated Name",
                EnterpriseId = enterpriseId,
                MentorId = newMentorUserId
            };

            var mentorUser = new User(newMentorUserId, "M001", "mentor@rikkei.com", "Mentor Rikkei", UserRole.Mentor, "hashed");
            var mentorEnterpriseUser = new EnterpriseUser
            {
                EnterpriseUserId = newMentorEnterpriseUserId,
                UserId = newMentorUserId,
                EnterpriseId = enterpriseId,
                User = mentorUser
            };
            var mockProjectRepo = new Mock<IGenericRepository<Project>>();
            var mockGroupMentorHistoryRepo = new Mock<IGenericRepository<GroupMentorHistory>>();

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup> { existingGroup }.BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>().ExistsAsync(It.IsAny<Expression<Func<InternshipPhase, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(x => x.Repository<Enterprise>().ExistsAsync(It.IsAny<Expression<Func<Enterprise, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query())
                .Returns(new List<EnterpriseUser> { mentorEnterpriseUser }.BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<Project>())
                .Returns(mockProjectRepo.Object);
            mockProjectRepo.Setup(x => x.Query())
                .Returns(new List<Project> { project }.BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<GroupMentorHistory>())
                .Returns(mockGroupMentorHistoryRepo.Object);
            mockGroupMentorHistoryRepo.Setup(x => x.AddAsync(It.IsAny<GroupMentorHistory>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GroupMentorHistory history, CancellationToken _) => history);

            mockProjectRepo.Setup(x => x.ExecuteUpdateAsync(
                    It.IsAny<Expression<Func<Project, bool>>>(),
                    It.IsAny<Expression<Func<SetPropertyCalls<Project>, SetPropertyCalls<Project>>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _mockMapper.Setup(x => x.Map<UpdateInternshipGroupResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new UpdateInternshipGroupResponse { InternshipId = internshipId, GroupName = "Updated Name" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockProjectRepo.Verify(x => x.ExecuteUpdateAsync(
                    It.IsAny<Expression<Func<Project, bool>>>(),
                    It.IsAny<Expression<Func<SetPropertyCalls<Project>, SetPropertyCalls<Project>>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeast(2));
        }

        [Fact]
        public async Task Handle_WhenMentorUnchanged_ShouldNotUpdateProjectMentorOwnership()
        {
            // Arrange
            var phaseId = Guid.NewGuid();
            var enterpriseId = Guid.NewGuid();
            var mentorUserId = Guid.NewGuid();
            var mentorEnterpriseUserId = Guid.NewGuid();

            var existingGroup = InternshipGroup.Create(
                phaseId,
                "Team A",
                enterpriseId: enterpriseId,
                mentorId: mentorEnterpriseUserId);

            var command = new UpdateInternshipGroupCommand
            {
                InternshipId = existingGroup.InternshipId,
                PhaseId = phaseId,
                GroupName = "Team A Updated",
                MentorId = mentorUserId
            };

            var mentorUser = new User(mentorUserId, "M002", "mentor.same@rikkei.com", "Mentor Same", UserRole.Mentor, "hashed");
            var mentorEnterpriseUser = new EnterpriseUser
            {
                EnterpriseUserId = mentorEnterpriseUserId,
                UserId = mentorUserId,
                EnterpriseId = enterpriseId,
                User = mentorUser
            };

            var mockProjectRepo = new Mock<IGenericRepository<Project>>();

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup> { existingGroup }.BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>().ExistsAsync(It.IsAny<Expression<Func<InternshipPhase, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Repository<Enterprise>().ExistsAsync(It.IsAny<Expression<Func<Enterprise, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query())
                .Returns(new List<EnterpriseUser> { mentorEnterpriseUser }.BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<Project>())
                .Returns(mockProjectRepo.Object);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockMapper.Setup(x => x.Map<UpdateInternshipGroupResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new UpdateInternshipGroupResponse { InternshipId = existingGroup.InternshipId, GroupName = "Team A Updated" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mockProjectRepo.Verify(x => x.ExecuteUpdateAsync(
                    It.IsAny<Expression<Func<Project, bool>>>(),
                    It.IsAny<Expression<Func<SetPropertyCalls<Project>, SetPropertyCalls<Project>>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
            _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WhenMentorDifferentEnterprise_ShouldReturnBadRequest()
        {
            // Arrange
            var phaseId = Guid.NewGuid();
            var groupEnterpriseId = Guid.NewGuid();
            var mentorEnterpriseId = Guid.NewGuid();
            var mentorUserId = Guid.NewGuid();

            var existingGroup = InternshipGroup.Create(
                phaseId,
                "Team B",
                enterpriseId: groupEnterpriseId,
                mentorId: Guid.NewGuid());

            var command = new UpdateInternshipGroupCommand
            {
                InternshipId = existingGroup.InternshipId,
                PhaseId = phaseId,
                GroupName = "Team B Updated",
                MentorId = mentorUserId
            };

            var mentorUser = new User(mentorUserId, "M003", "mentor.other@fpt.com", "Mentor Other", UserRole.Mentor, "hashed");
            var mentorEnterpriseUser = new EnterpriseUser
            {
                EnterpriseUserId = Guid.NewGuid(),
                UserId = mentorUserId,
                EnterpriseId = mentorEnterpriseId,
                User = mentorUser
            };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup> { existingGroup }.BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>().ExistsAsync(It.IsAny<Expression<Func<InternshipPhase, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Repository<Enterprise>().ExistsAsync(It.IsAny<Expression<Func<Enterprise, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query())
                .Returns(new List<EnterpriseUser> { mentorEnterpriseUser }.BuildMock());
            _mockMessageService.Setup(x => x.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise))
                .Returns("Mentor must belong to the same enterprise as group.");

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

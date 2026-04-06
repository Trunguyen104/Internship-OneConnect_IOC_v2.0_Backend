using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Commands.AssignMentorToGroup;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace IOCv2.Tests.Features.InternshipGroups.Commands
{
    public class AssignMentorToGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<INotificationPushService> _mockPushService;
        private readonly Mock<ILogger<AssignMentorToGroupHandler>> _mockLogger;

        private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
        private readonly Mock<IGenericRepository<InternshipGroup>> _mockGroupRepo;
        private readonly Mock<IGenericRepository<Project>> _mockProjectRepo;
        private readonly Mock<IGenericRepository<GroupMentorHistory>> _mockHistoryRepo;
        private readonly Mock<IGenericRepository<InternshipStudent>> _mockInternshipStudentRepo;
        private readonly Mock<IGenericRepository<Notification>> _mockNotificationRepo;

        private readonly AssignMentorToGroupHandler _handler;

        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _enterpriseId = Guid.NewGuid();
        private readonly Guid _callerEnterpriseUserId = Guid.NewGuid();
        private readonly Guid _mentorEnterpriseUserId = Guid.NewGuid();

        public AssignMentorToGroupHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockPushService = new Mock<INotificationPushService>();
            _mockLogger = new Mock<ILogger<AssignMentorToGroupHandler>>();

            _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();
            _mockGroupRepo = new Mock<IGenericRepository<InternshipGroup>>();
            _mockProjectRepo = new Mock<IGenericRepository<Project>>();
            _mockHistoryRepo = new Mock<IGenericRepository<GroupMentorHistory>>();
            _mockInternshipStudentRepo = new Mock<IGenericRepository<InternshipStudent>>();
            _mockNotificationRepo = new Mock<IGenericRepository<Notification>>();

            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(_mockGroupRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Project>()).Returns(_mockProjectRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<GroupMentorHistory>()).Returns(_mockHistoryRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<InternshipStudent>()).Returns(_mockInternshipStudentRepo.Object);
            _mockUnitOfWork.Setup(x => x.Repository<Notification>()).Returns(_mockNotificationRepo.Object);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>(), It.IsAny<object[]>())).Returns((string key, object[] _) => key);

            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

            _mockPushService.Setup(x => x.PushNewNotificationAsync(It.IsAny<Guid>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: valid current user (caller = HR/admin in the enterprise)
            _mockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId.ToString());
            _mockCurrentUserService.Setup(x => x.Role).Returns(UserRole.HR.ToString());

            // Default: caller enterprise user exists
            _mockEnterpriseUserRepo.Setup(x => x.Query()).Returns(new List<EnterpriseUser>
            {
                new EnterpriseUser
                {
                    UserId = _currentUserId,
                    EnterpriseId = _enterpriseId,
                    EnterpriseUserId = _callerEnterpriseUserId
                }
            }.AsQueryable().BuildMock());

            // Default: no students, no notifications
            _mockInternshipStudentRepo.Setup(x => x.Query())
                .Returns(new List<InternshipStudent>().AsQueryable().BuildMock());

            _mockNotificationRepo.Setup(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Notification n, CancellationToken _) => n);
            _mockNotificationRepo.Setup(x => x.CountAsync(It.IsAny<Expression<Func<Notification, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Default: history AddAsync
            _mockHistoryRepo.Setup(x => x.AddAsync(It.IsAny<GroupMentorHistory>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GroupMentorHistory h, CancellationToken _) => h);

            // Default: group UpdateAsync
            _mockGroupRepo.Setup(x => x.UpdateAsync(It.IsAny<InternshipGroup>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: project ExecuteUpdateAsync
            _mockProjectRepo.Setup(x => x.ExecuteUpdateAsync(
                It.IsAny<Expression<Func<Project, bool>>>(),
                It.IsAny<Expression<Func<SetPropertyCalls<Project>, SetPropertyCalls<Project>>>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Default: no active projects in group
            _mockProjectRepo.Setup(x => x.Query())
                .Returns(new List<Project>().AsQueryable().BuildMock());

            _handler = new AssignMentorToGroupHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockCacheService.Object,
                _mockPushService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_InvalidUserId_ReturnsUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.UserId).Returns("not-a-valid-guid");
            var command = new AssignMentorToGroupCommand(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }

        [Fact]
        public async Task Handle_CallerEnterpriseUserNotFound_ReturnsForbidden()
        {
            // Arrange
            _mockEnterpriseUserRepo.Setup(x => x.Query())
                .Returns(new List<EnterpriseUser>().AsQueryable().BuildMock());

            var command = new AssignMentorToGroupCommand(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Forbidden);
        }

        [Fact]
        public async Task Handle_GroupNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());

            var command = new AssignMentorToGroupCommand(Guid.NewGuid(), Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_GroupNotActive_ReturnsBadRequest()
        {
            // Arrange
            var group = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Archived Group",
                enterpriseId: _enterpriseId);
            group.UpdateStatus(GroupStatus.Archived);

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            var command = new AssignMentorToGroupCommand(group.InternshipId, Guid.NewGuid());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_FinishedGroupBeforeEndDate_AllowsAssign()
        {
            // Arrange
            var mentorUserId = Guid.NewGuid();

            var group = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Finished But Still In Date",
                enterpriseId: _enterpriseId,
                startDate: DateTime.UtcNow.AddDays(-1),
                endDate: DateTime.UtcNow.AddDays(7));
            group.UpdateStatus(GroupStatus.Finished);

            var mentorUser = new User(mentorUserId, "MNT-001", "mentor@test.com", "Mentor User", UserRole.Mentor, "hash");
            var mentorEnterpriseUser = new EnterpriseUser
            {
                UserId = mentorUserId,
                EnterpriseId = _enterpriseId,
                EnterpriseUserId = _mentorEnterpriseUserId,
                User = mentorUser
            };

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            var callCount = 0;
            _mockEnterpriseUserRepo.Setup(x => x.Query())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return new List<EnterpriseUser>
                        {
                            new EnterpriseUser
                            {
                                UserId = _currentUserId,
                                EnterpriseId = _enterpriseId,
                                EnterpriseUserId = _callerEnterpriseUserId
                            }
                        }.AsQueryable().BuildMock();
                    }

                    return new List<EnterpriseUser> { mentorEnterpriseUser }.AsQueryable().BuildMock();
                });

            var command = new AssignMentorToGroupCommand(group.InternshipId, mentorUserId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.MentorUserId.Should().Be(mentorUserId);
        }

        [Fact]
        public async Task Handle_MentorNotFound_ReturnsNotFound()
        {
            // Arrange
            var group = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Active Group",
                enterpriseId: _enterpriseId);
            // Status is Active by default from Create()

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            // Caller found, but no mentor enterprise user for the given mentorUserId
            // We need two separate Query() setups: one returns caller, one returns nothing for mentor.
            // The handler calls Repository<EnterpriseUser>().Query() twice:
            //   1st call: find caller (UserId == currentUserId)
            //   2nd call: find mentor (UserId == request.MentorUserId)
            var mentorUserId = Guid.NewGuid();

            var callCount = 0;
            _mockEnterpriseUserRepo.Setup(x => x.Query())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return new List<EnterpriseUser>
                        {
                            new EnterpriseUser
                            {
                                UserId = _currentUserId,
                                EnterpriseId = _enterpriseId,
                                EnterpriseUserId = _callerEnterpriseUserId
                            }
                        }.AsQueryable().BuildMock();
                    }
                    // second call: no mentor found
                    return new List<EnterpriseUser>().AsQueryable().BuildMock();
                });

            var command = new AssignMentorToGroupCommand(group.InternshipId, mentorUserId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }

        [Fact]
        public async Task Handle_ValidFirstAssign_ReturnsSuccess()
        {
            // Arrange
            var mentorUserId = Guid.NewGuid();

            var group = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Active Group",
                enterpriseId: _enterpriseId,
                mentorId: null);   // no mentor yet — first assign
            // Status is Active by default from Create()

            var mentorUser = new User(mentorUserId, "MNT-001", "mentor@test.com", "Mentor User", UserRole.Mentor, "hash");
            var mentorEnterpriseUser = new EnterpriseUser
            {
                UserId = mentorUserId,
                EnterpriseId = _enterpriseId,
                EnterpriseUserId = _mentorEnterpriseUserId,
                User = mentorUser
            };

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            // Two sequential calls to Repository<EnterpriseUser>().Query():
            //   1st: find caller by _currentUserId
            //   2nd: find mentor by mentorUserId
            var callCount = 0;
            _mockEnterpriseUserRepo.Setup(x => x.Query())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return new List<EnterpriseUser>
                        {
                            new EnterpriseUser
                            {
                                UserId = _currentUserId,
                                EnterpriseId = _enterpriseId,
                                EnterpriseUserId = _callerEnterpriseUserId
                            }
                        }.AsQueryable().BuildMock();
                    }
                    return new List<EnterpriseUser> { mentorEnterpriseUser }.AsQueryable().BuildMock();
                });

            var command = new AssignMentorToGroupCommand(group.InternshipId, mentorUserId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.InternshipGroupId.Should().Be(group.InternshipId);
            result.Data.MentorUserId.Should().Be(mentorUserId);
            result.Data.MentorFullName.Should().Be("Mentor User");
            result.Data.MentorEmail.Should().Be("mentor@test.com");
            result.Data.ActionType.Should().Be(MentorActionType.Assign.ToString());

            _mockGroupRepo.Verify(x => x.UpdateAsync(It.IsAny<InternshipGroup>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockHistoryRepo.Verify(x => x.AddAsync(It.IsAny<GroupMentorHistory>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_AssignSameMentor_ReturnsBadRequest()
        {
            // Arrange
            var mentorUserId = Guid.NewGuid();

            var group = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Active Group",
                enterpriseId: _enterpriseId,
                mentorId: _mentorEnterpriseUserId); // already assigned to this mentor

            var mentorUser = new User(mentorUserId, "MNT-001", "mentor@test.com", "Mentor User", UserRole.Mentor, "hash");
            var mentorEnterpriseUser = new EnterpriseUser
            {
                UserId = mentorUserId,
                EnterpriseId = _enterpriseId,
                EnterpriseUserId = _mentorEnterpriseUserId,
                User = mentorUser
            };

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            var callCount = 0;
            _mockEnterpriseUserRepo.Setup(x => x.Query())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return new List<EnterpriseUser>
                        {
                            new EnterpriseUser
                            {
                                UserId = _currentUserId,
                                EnterpriseId = _enterpriseId,
                                EnterpriseUserId = _callerEnterpriseUserId
                            }
                        }.AsQueryable().BuildMock();
                    }
                    return new List<EnterpriseUser> { mentorEnterpriseUser }.AsQueryable().BuildMock();
                });

            var command = new AssignMentorToGroupCommand(group.InternshipId, mentorUserId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUnitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_ValidReassign_ReturnsSuccessWithChangeAction()
        {
            // Arrange
            var oldMentorEnterpriseUserId = Guid.NewGuid();
            var oldMentorUserId = Guid.NewGuid();
            var newMentorUserId = Guid.NewGuid();

            var group = InternshipGroup.Create(
                phaseId: Guid.NewGuid(),
                groupName: "Active Group",
                enterpriseId: _enterpriseId,
                mentorId: oldMentorEnterpriseUserId);

            var newMentorUser = new User(newMentorUserId, "MNT-NEW", "newmentor@test.com", "New Mentor", UserRole.Mentor, "hash");
            var newMentorEnterpriseUser = new EnterpriseUser
            {
                UserId = newMentorUserId,
                EnterpriseId = _enterpriseId,
                EnterpriseUserId = _mentorEnterpriseUserId,
                User = newMentorUser
            };

            var oldMentorUser = new User(oldMentorUserId, "MNT-OLD", "oldmentor@test.com", "Old Mentor", UserRole.Mentor, "hash");
            var oldMentorEnterpriseUser = new EnterpriseUser
            {
                UserId = oldMentorUserId,
                EnterpriseId = _enterpriseId,
                EnterpriseUserId = oldMentorEnterpriseUserId,
                User = oldMentorUser
            };

            _mockGroupRepo.Setup(x => x.Query())
                .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

            var callCount = 0;
            _mockEnterpriseUserRepo.Setup(x => x.Query())
                .Returns(() =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        return new List<EnterpriseUser>
                        {
                            new EnterpriseUser
                            {
                                UserId = _currentUserId,
                                EnterpriseId = _enterpriseId,
                                EnterpriseUserId = _callerEnterpriseUserId
                            }
                        }.AsQueryable().BuildMock();
                    }

                    if (callCount == 2)
                    {
                        return new List<EnterpriseUser> { newMentorEnterpriseUser }.AsQueryable().BuildMock();
                    }

                    return new List<EnterpriseUser> { oldMentorEnterpriseUser }.AsQueryable().BuildMock();
                });

            var command = new AssignMentorToGroupCommand(group.InternshipId, newMentorUserId);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.ActionType.Should().Be(MentorActionType.Change.ToString());
            result.Data.MentorUserId.Should().Be(newMentorUserId);

            _mockGroupRepo.Verify(x => x.UpdateAsync(It.IsAny<InternshipGroup>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockHistoryRepo.Verify(x => x.AddAsync(It.IsAny<GroupMentorHistory>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}

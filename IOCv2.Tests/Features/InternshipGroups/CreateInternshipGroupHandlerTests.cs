using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup;
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
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.InternshipGroups
{
    public class CreateInternshipGroupHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<CreateInternshipGroupHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly CreateInternshipGroupHandler _handler;

        private readonly Guid _currentUserId = Guid.NewGuid();
        private readonly Guid _enterpriseId = Guid.NewGuid();
        private readonly Guid _enterpriseUserId = Guid.NewGuid();

        public CreateInternshipGroupHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CreateInternshipGroupHandler>>();
            _mockCacheService = new Mock<ICacheService>();

            // Default: user hợp lệ + là EnterpriseUser
            _mockCurrentUserService.Setup(x => x.UserId).Returns(_currentUserId.ToString());

            var enterpriseUsers = new List<EnterpriseUser>
            {
                new EnterpriseUser
                {
                    UserId = _currentUserId,
                    EnterpriseId = _enterpriseId,
                    EnterpriseUserId = _enterpriseUserId
                }
            };
            _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>().Query())
                .Returns(enterpriseUsers.AsQueryable().BuildMock());

            // Default message service returns key as value
            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns((string key) => key);

            _handler = new CreateInternshipGroupHandler(
                _mockUnitOfWork.Object,
                _mockCurrentUserService.Object,
                _mockMessageService.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockCacheService.Object);
        }

        private static InternshipPhase CreateOpenPhase(Guid phaseId, Guid enterpriseId)
        {
            var phase = InternshipPhase.Create(enterpriseId, "Test Phase",
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)), null, null);
            typeof(InternshipPhase).GetProperty("PhaseId")!.SetValue(phase, phaseId);
            typeof(InternshipPhase).GetProperty("Status")!.SetValue(phase, InternshipPhaseStatus.Open);
            return phase;
        }

        [Fact]
        public async Task Handle_Request_NoStudents_ShouldReturnBadRequest()
        {
            // Arrange
            var phaseId = Guid.NewGuid();
            var activePhase = CreateOpenPhase(phaseId, _enterpriseId);

            var command = new CreateInternshipGroupCommand
            {
                PhaseId = phaseId,
                GroupName = "Test Group",
                EnterpriseId = _enterpriseId,
                Students = new List<CreateInternshipStudentDto>() // Rỗng
            };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>().Query())
                .Returns(new List<InternshipPhase> { activePhase }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().AddAsync(It.IsAny<InternshipGroup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((InternshipGroup g, CancellationToken c) => g);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
            _mockUnitOfWork.Verify(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ValidRequest_WithApprovedStudents_ShouldReturnSuccess()
        {
            // Arrange
            var phaseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var activePhase = CreateOpenPhase(phaseId, _enterpriseId);

            var command = new CreateInternshipGroupCommand
            {
                PhaseId = phaseId,
                GroupName = "Test Group",
                EnterpriseId = _enterpriseId,
                Students = new List<CreateInternshipStudentDto>
                {
                    new CreateInternshipStudentDto { StudentId = studentId, Role = InternshipRole.Leader }
                }
            };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>().Query())
                .Returns(new List<InternshipPhase> { activePhase }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<Student>().FindAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Student> { new Student { StudentId = studentId, UserId = Guid.NewGuid() } });

            var approvedApps = new List<IOCv2.Domain.Entities.InternshipApplication>
            {
                new IOCv2.Domain.Entities.InternshipApplication
                {
                    StudentId = studentId,
                    EnterpriseId = _enterpriseId,
                    TermId = Guid.NewGuid(),
                    Status = InternshipApplicationStatus.Approved
                }
            };
            _mockUnitOfWork.Setup(x => x.Repository<IOCv2.Domain.Entities.InternshipApplication>().Query())
                .Returns(approvedApps.AsQueryable().BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
                .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());

            _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().AddAsync(It.IsAny<InternshipGroup>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((InternshipGroup g, CancellationToken c) => g);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockMapper.Setup(x => x.Map<CreateInternshipGroupResponse>(It.IsAny<InternshipGroup>()))
                .Returns(new CreateInternshipGroupResponse { GroupName = "Test Group" });

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_InvalidUserId_ShouldReturnUnauthorized()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.UserId).Returns("invalid-guid");
            var command = new CreateInternshipGroupCommand { PhaseId = Guid.NewGuid(), GroupName = "Test" };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
        }

        [Fact]
        public async Task Handle_StudentNotApproved_ShouldReturnBadRequest()
        {
            // Arrange
            var phaseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var activePhase = CreateOpenPhase(phaseId, _enterpriseId);

            var command = new CreateInternshipGroupCommand
            {
                PhaseId = phaseId,
                GroupName = "Test Group",
                EnterpriseId = _enterpriseId,
                Students = new List<CreateInternshipStudentDto>
                {
                    new CreateInternshipStudentDto { StudentId = studentId, Role = InternshipRole.Member }
                }
            };

            _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>().Query())
                .Returns(new List<InternshipPhase> { activePhase }.AsQueryable().BuildMock());
            _mockUnitOfWork.Setup(x => x.Repository<Student>().FindAsync(It.IsAny<Expression<Func<Student, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Student> { new Student { StudentId = studentId } });

            // Không có approved application → trả về danh sách rỗng
            _mockUnitOfWork.Setup(x => x.Repository<IOCv2.Domain.Entities.InternshipApplication>().Query())
                .Returns(new List<IOCv2.Domain.Entities.InternshipApplication>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        }

        [Fact]
        public async Task Handle_TermNotFound_ShouldReturnNotFound()
        {
            // Arrange
            var command = new CreateInternshipGroupCommand { PhaseId = Guid.NewGuid(), GroupName = "Test" };

            // Trả về list rỗng → FirstOrDefaultAsync sẽ ra null
            _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>().Query())
                .Returns(new List<InternshipPhase>().AsQueryable().BuildMock());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);
        }
    }
}

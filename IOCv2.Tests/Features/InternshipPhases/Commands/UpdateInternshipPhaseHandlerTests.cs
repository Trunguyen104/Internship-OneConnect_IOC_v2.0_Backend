using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipPhases.Commands.UpdateInternshipPhase;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace IOCv2.Tests.Features.InternshipPhases.Commands;

public class UpdateInternshipPhaseHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<ILogger<UpdateInternshipPhaseHandler>> _mockLogger;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IGenericRepository<InternshipPhase>> _mockPhaseRepo;
    private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
    private readonly UpdateInternshipPhaseHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _phaseId = Guid.NewGuid();

    public UpdateInternshipPhaseHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<UpdateInternshipPhaseHandler>>();
        _mockCacheService = new Mock<ICacheService>();

        _mockPhaseRepo = new Mock<IGenericRepository<InternshipPhase>>();
        _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();

        _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>()).Returns(_mockPhaseRepo.Object);
        _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);

        _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _handler = new UpdateInternshipPhaseHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            _mockLogger.Object,
            _mockCacheService.Object);
    }

    private InternshipPhase CreateActivePhase(Guid? phaseId = null)
    {
        var phase = InternshipPhase.Create(
            _enterpriseId,
            "Phase Name",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            "Software Engineering",
            10,
            "Description");

        var id = phaseId ?? _phaseId;
        typeof(InternshipPhase).GetProperty("PhaseId")!.SetValue(phase, id);
        typeof(InternshipPhase).GetProperty("Status")!.SetValue(phase, InternshipPhaseStatus.Open);

        return phase;
    }

    [Fact]
    public async Task Handle_PhaseNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase>().AsQueryable().BuildMock());

        var command = new UpdateInternshipPhaseCommand
        {
            PhaseId = _phaseId,
            Name = "New Name",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            MajorFields = "Software Engineering",
            Capacity = 10
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_PhaseEnded_ReturnsBadRequest()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        var endedPhase = InternshipPhase.Create(
            _enterpriseId,
            "Phase Name",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-100)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            "Software Engineering",
            10,
            "Description");
        typeof(InternshipPhase).GetProperty("PhaseId")!.SetValue(endedPhase, _phaseId);

        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase> { endedPhase }.AsQueryable().BuildMock());

        var command = new UpdateInternshipPhaseCommand
        {
            PhaseId = _phaseId,
            Name = "New Name",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-100)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            MajorFields = "Software Engineering",
            Capacity = 10
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BadRequest);
    }

    [Fact]
    public async Task Handle_LockedFieldChangeWithGroups_ReturnsBadRequest()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        var phase = CreateActivePhase();

        // Add a non-deleted group via reflection
        var group = InternshipGroup.Create(phase.PhaseId, "Group A");
        typeof(InternshipGroup).GetProperty("DeletedAt")!.SetValue(group, null);
        var groups = new List<InternshipGroup> { group };
        typeof(InternshipPhase).GetProperty("InternshipGroups")!.SetValue(phase, groups);

        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase> { phase }.AsQueryable().BuildMock());

        // Changing StartDate → locked field change
        var command = new UpdateInternshipPhaseCommand
        {
            PhaseId = _phaseId,
            Name = phase.Name,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)), // changed
            EndDate = phase.EndDate,
            MajorFields = phase.MajorFields,
            Capacity = phase.Capacity
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BadRequest);
    }

    [Fact]
    public async Task Handle_NoChanges_ReturnsSuccess()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        var phase = CreateActivePhase();

        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase> { phase }.AsQueryable().BuildMock());

        // Command with exact same values as the phase
        var command = new UpdateInternshipPhaseCommand
        {
            PhaseId = _phaseId,
            Name = phase.Name,
            StartDate = phase.StartDate,
            EndDate = phase.EndDate,
            MajorFields = phase.MajorFields,
            Capacity = phase.Capacity,
            Description = phase.Description
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _mockPhaseRepo.Verify(x => x.UpdateAsync(It.IsAny<InternshipPhase>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SuperAdmin_ValidUpdate_ReturnsSuccess()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        var phase = CreateActivePhase();

        // Setup Query for the phase (called twice: once to find phase, once for duplicate name check)
        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase> { phase }.AsQueryable().BuildMock());

        _mockPhaseRepo.Setup(x => x.UpdateAsync(It.IsAny<InternshipPhase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateInternshipPhaseCommand
        {
            PhaseId = _phaseId,
            Name = "Updated Phase Name",
            StartDate = phase.StartDate,
            EndDate = phase.EndDate,
            MajorFields = phase.MajorFields,
            Capacity = phase.Capacity,
            Description = "Updated description"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Updated Phase Name");
        _mockPhaseRepo.Verify(x => x.UpdateAsync(It.IsAny<InternshipPhase>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

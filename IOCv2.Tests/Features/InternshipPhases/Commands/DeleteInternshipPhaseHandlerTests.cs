using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipPhases.Commands.DeleteInternshipPhase;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace IOCv2.Tests.Features.InternshipPhases.Commands;

public class DeleteInternshipPhaseHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<ILogger<DeleteInternshipPhaseHandler>> _mockLogger;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IGenericRepository<InternshipPhase>> _mockPhaseRepo;
    private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
    private readonly DeleteInternshipPhaseHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();
    private readonly Guid _phaseId = Guid.NewGuid();

    public DeleteInternshipPhaseHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<DeleteInternshipPhaseHandler>>();
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

        _handler = new DeleteInternshipPhaseHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            _mockLogger.Object,
            _mockCacheService.Object);
    }

    private InternshipPhase CreatePhaseWithNoGroups()
    {
        var phase = InternshipPhase.Create(
            _enterpriseId,
            "Phase Name",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            "Software Engineering",
            10,
            "Description");

        typeof(InternshipPhase).GetProperty("PhaseId")!.SetValue(phase, _phaseId);
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

        var command = new DeleteInternshipPhaseCommand(_phaseId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_PhaseHasActiveGroups_ReturnsBadRequest()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        var phase = CreatePhaseWithNoGroups();

        // Add an active (non-deleted) group via reflection
        var group = InternshipGroup.Create(phase.PhaseId, "Group A");
        typeof(InternshipGroup).GetProperty("DeletedAt")!.SetValue(group, null);
        var groups = new List<InternshipGroup> { group };
        typeof(InternshipPhase).GetProperty("InternshipGroups")!.SetValue(phase, groups);

        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase> { phase }.AsQueryable().BuildMock());

        var command = new DeleteInternshipPhaseCommand(_phaseId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BadRequest);
    }

    [Fact]
    public async Task Handle_SuperAdmin_ValidDelete_ReturnsSuccess()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        var phase = CreatePhaseWithNoGroups();

        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase> { phase }.AsQueryable().BuildMock());

        _mockPhaseRepo.Setup(x => x.HardDeleteAsync(It.IsAny<InternshipPhase>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new DeleteInternshipPhaseCommand(_phaseId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        _mockPhaseRepo.Verify(x => x.HardDeleteAsync(It.IsAny<InternshipPhase>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

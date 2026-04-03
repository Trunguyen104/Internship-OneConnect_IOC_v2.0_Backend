using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipPhases.Commands.CreateInternshipPhase;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;

namespace IOCv2.Tests.Features.InternshipPhases.Commands;

public class CreateInternshipPhaseHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly Mock<ILogger<CreateInternshipPhaseHandler>> _mockLogger;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IGenericRepository<InternshipPhase>> _mockPhaseRepo;
    private readonly Mock<IGenericRepository<Enterprise>> _mockEnterpriseRepo;
    private readonly Mock<IGenericRepository<EnterpriseUser>> _mockEnterpriseUserRepo;
    private readonly CreateInternshipPhaseHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _enterpriseId = Guid.NewGuid();

    public CreateInternshipPhaseHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockMessageService = new Mock<IMessageService>();
        _mockLogger = new Mock<ILogger<CreateInternshipPhaseHandler>>();
        _mockCacheService = new Mock<ICacheService>();

        _mockPhaseRepo = new Mock<IGenericRepository<InternshipPhase>>();
        _mockEnterpriseRepo = new Mock<IGenericRepository<Enterprise>>();
        _mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();

        _mockUnitOfWork.Setup(x => x.Repository<InternshipPhase>()).Returns(_mockPhaseRepo.Object);
        _mockUnitOfWork.Setup(x => x.Repository<Enterprise>()).Returns(_mockEnterpriseRepo.Object);
        _mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>()).Returns(_mockEnterpriseUserRepo.Object);

        _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _handler = new CreateInternshipPhaseHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            _mockLogger.Object,
            _mockCacheService.Object);
    }

    [Fact]
    public async Task Handle_StartDateInPast_ReturnsBadRequest()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        var command = new CreateInternshipPhaseCommand
        {
            EnterpriseId = _enterpriseId,
            Name = "Past Phase",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(60)),
            MajorFields = "Software Engineering",
            Capacity = 10
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.BadRequest);
        _mockPhaseRepo.Verify(x => x.AddAsync(It.IsAny<InternshipPhase>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("EnterpriseUser");
        _mockCurrentUserService.Setup(x => x.UserId).Returns("not-a-valid-guid");

        var command = new CreateInternshipPhaseCommand
        {
            EnterpriseId = _enterpriseId,
            Name = "Phase 1",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            MajorFields = "Software Engineering",
            Capacity = 10
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_EnterpriseUserNotFound_ReturnsForbidden()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("EnterpriseUser");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        _mockEnterpriseUserRepo.Setup(x => x.Query())
            .Returns(new List<EnterpriseUser>().AsQueryable().BuildMock());

        var command = new CreateInternshipPhaseCommand
        {
            EnterpriseId = _enterpriseId,
            Name = "Phase 1",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            MajorFields = "Software Engineering",
            Capacity = 10
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }

    [Fact]
    public async Task Handle_EnterpriseNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        _mockEnterpriseRepo.Setup(x => x.ExistsAsync(
                It.IsAny<Expression<Func<Enterprise, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new CreateInternshipPhaseCommand
        {
            EnterpriseId = _enterpriseId,
            Name = "Phase 1",
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
    public async Task Handle_DuplicateName_ReturnsBadRequest()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        _mockEnterpriseRepo.Setup(x => x.ExistsAsync(
                It.IsAny<Expression<Func<Enterprise, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var existingPhase = InternshipPhase.Create(
            _enterpriseId,
            "Phase 1",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            "Software Engineering",
            10,
            "Description",
            null);

        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase> { existingPhase }.AsQueryable().BuildMock());

        var command = new CreateInternshipPhaseCommand
        {
            EnterpriseId = _enterpriseId,
            Name = "Phase 1",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
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
    public async Task Handle_SuperAdmin_ValidCreate_ReturnsSuccess()
    {
        // Arrange
        _mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        _mockCurrentUserService.Setup(x => x.UserId).Returns(_userId.ToString());

        _mockEnterpriseRepo.Setup(x => x.ExistsAsync(
                It.IsAny<Expression<Func<Enterprise, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockPhaseRepo.Setup(x => x.Query())
            .Returns(new List<InternshipPhase>().AsQueryable().BuildMock());

        _mockPhaseRepo.Setup(x => x.AddAsync(It.IsAny<InternshipPhase>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InternshipPhase p, CancellationToken _) => p);

        var command = new CreateInternshipPhaseCommand
        {
            EnterpriseId = _enterpriseId,
            Name = "Phase 1",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(100)),
            MajorFields = "Software Engineering",
            Capacity = 10,
            Description = "Test description"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be("Phase 1");
        result.Data.EnterpriseId.Should().Be(_enterpriseId);
        _mockPhaseRepo.Verify(x => x.AddAsync(It.IsAny<InternshipPhase>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

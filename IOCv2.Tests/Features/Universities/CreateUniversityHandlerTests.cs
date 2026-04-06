using FluentAssertions;
using IOCv2.Application.Features.Universities.Commands.CreateUniversity;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IOCv2.Tests.Features.Universities;

public class CreateUniversityHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<CreateUniversityHandler>> _mockLogger;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IGenericRepository<University>> _mockUniversityRepository;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly CreateUniversityHandler _handler;

    public CreateUniversityHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<CreateUniversityHandler>>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockUniversityRepository = new Mock<IGenericRepository<University>>();
        _mockCacheService = new Mock<ICacheService>();
        _mockEmailService = new Mock<IEmailService>();

        _mockUnitOfWork.Setup(u => u.Repository<University>()).Returns(_mockUniversityRepository.Object);
        _mockUnitOfWork.Setup(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockUnitOfWork.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _mockUniversityRepository.Setup(r => r.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<University, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockUniversityRepository.Setup(r => r.AddAsync(It.IsAny<University>(), It.IsAny<CancellationToken>())).ReturnsAsync((University u, CancellationToken _) => u);
        _mockCacheService.Setup(c => c.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _mockBackgroundEmailSender
            .Setup(s => s.EnqueueUniversityCreationEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        _handler = new CreateUniversityHandler(
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockCurrentUserService.Object,
            _mockCacheService.Object,
            _mockEmailService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesUniversityAndReturnsId()
    {
        // Arrange
        var command = new CreateUniversityCommand
        {
            Code = "TEST",
            Name = "Test University",
            Address = "Test Address"
        };

        _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid().ToString());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeEmpty();
        
        _mockUniversityRepository.Verify(r => r.AddAsync(It.IsAny<University>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenExceptionOccurs_RollbacksTransaction()
    {
        // Arrange
        var command = new CreateUniversityCommand { Code = "ERR", Name = "Error U" };
        _mockUnitOfWork.Setup(u => u.SaveChangeAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB Error"));

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<Exception>();
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

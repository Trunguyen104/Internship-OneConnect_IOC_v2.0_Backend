using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Universities.Queries.GetUniversities;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using MockQueryable;
using MockQueryable.EntityFrameworkCore;
using Moq;
using Xunit;

namespace IOCv2.Tests.Features.Universities;

public class GetUniversitiesHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<GetUniversitiesHandler>> _mockLogger;
    private readonly Mock<IGenericRepository<University>> _mockUniversityRepository;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly GetUniversitiesHandler _handler;

    public GetUniversitiesHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<GetUniversitiesHandler>>();
        _mockUniversityRepository = new Mock<IGenericRepository<University>>();
        _mockCacheService = new Mock<ICacheService>();

        _mockUnitOfWork.Setup(u => u.Repository<University>()).Returns(_mockUniversityRepository.Object);

        _handler = new GetUniversitiesHandler(_mockUnitOfWork.Object, _mockLogger.Object, _mockCacheService.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPaginatedUniversities()
    {
        // Arrange
        var universities = new List<University>
        {
            University.Create("HUST", "Hanoi University of Science and Technology", "Hanoi", null),
            University.Create("NEU", "National Economics University", "Hanoi", null)
        }.AsQueryable().BuildMock();

        _mockUniversityRepository.Setup(r => r.Query()).Returns(universities);

        var query = new GetUniversitiesQuery { PageNumber = 1, PageSize = 10 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(2);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Start GetUniversitiesQuery")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ReturnsFilteredUniversities()
    {
        // Arrange
        var universities = new List<University>
        {
            University.Create("HUST", "Hanoi University of Science and Technology", "Hanoi", null),
            University.Create("NEU", "National Economics University", "Hanoi", null)
        }.AsQueryable().BuildMock();

        _mockUniversityRepository.Setup(r => r.Query()).Returns(universities);

        var query = new GetUniversitiesQuery { SearchTerm = "Hanoi" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items[0].Code.Should().Be("HUST");
    }
}

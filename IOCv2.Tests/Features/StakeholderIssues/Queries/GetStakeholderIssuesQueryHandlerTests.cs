using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues;
using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IOCv2.Tests.Features.StakeholderIssues.Queries;

public class GetStakeholderIssuesQueryHandlerTests
{
    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedResult()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockLogger = new Mock<ILogger<GetStakeholderIssuesQueryHandler>>();
        var mockCacheService = new Mock<ICacheService>();

        var cached = PaginatedResult<GetStakeholderIssuesResponse>.Create(
            new List<GetStakeholderIssuesResponse> { new() { Title = "Issue A" } },
            1,
            1,
            10);

        mockCacheService
            .Setup(x => x.GetAsync<PaginatedResult<GetStakeholderIssuesResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var handler = new GetStakeholderIssuesQueryHandler(
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockLogger.Object,
            mockCacheService.Object);

        var query = new GetStakeholderIssuesQuery();

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().ContainSingle();
    }
}



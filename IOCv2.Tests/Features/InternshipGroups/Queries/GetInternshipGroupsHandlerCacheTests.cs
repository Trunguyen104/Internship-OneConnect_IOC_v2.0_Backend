using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.InternshipGroups.Queries;

public class GetInternshipGroupsHandlerCacheTests
{
    [Fact]
    public async Task Handle_UsesCache_WhenAvailable()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<IOCv2.Application.Common.Models.PaginatedResult<GetInternshipGroupsResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IOCv2.Application.Common.Models.PaginatedResult<GetInternshipGroupsResponse>(
                new List<GetInternshipGroupsResponse> { new() { GroupName = "Cached Group" } },
                1,
                1,
                10));

        var handler = new GetInternshipGroupsHandler(
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IMapper>(),
            cache.Object,
            Mock.Of<ILogger<GetInternshipGroupsHandler>>());

        var result = await handler.Handle(new GetInternshipGroupsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle(x => x.GroupName == "Cached Group");
    }

    [Fact]
    public async Task Handle_FetchesAndCaches_WhenCacheMiss()
    {
        var groups = new List<InternshipGroup> { InternshipGroup.Create(Guid.NewGuid(), "G1") };
        var repo = new Mock<IGenericRepository<InternshipGroup>>();
        repo.Setup(x => x.Query()).Returns(groups.AsQueryable().BuildMock());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<InternshipGroup>()).Returns(repo.Object);

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<IOCv2.Application.Common.Models.PaginatedResult<GetInternshipGroupsResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IOCv2.Application.Common.Models.PaginatedResult<GetInternshipGroupsResponse>?)null);

        var cfg = new MapperConfiguration(cfg =>
            cfg.CreateMap<InternshipGroup, GetInternshipGroupsResponse>());
        var mapper = cfg.CreateMapper();

        var handler = new GetInternshipGroupsHandler(
            uow.Object,
            mapper,
            cache.Object,
            Mock.Of<ILogger<GetInternshipGroupsHandler>>());

        var result = await handler.Handle(new GetInternshipGroupsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<IOCv2.Application.Common.Models.PaginatedResult<GetInternshipGroupsResponse>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
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
    private static readonly Guid _superAdminId = Guid.NewGuid();

    // Helper: tạo mock ICurrentUserService mặc định là SuperAdmin
    private static Mock<ICurrentUserService> SuperAdminUser()
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(_superAdminId.ToString());
        mock.Setup(x => x.Role).Returns("SuperAdmin");
        return mock;
    }

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
            Mock.Of<ILogger<GetInternshipGroupsHandler>>(),
            SuperAdminUser().Object,
            Mock.Of<IMessageService>());

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

        var mapperCfg = new MapperConfiguration(c => c.AddProfile<MappingProfile>());
        var mapper = mapperCfg.CreateMapper();

        var handler = new GetInternshipGroupsHandler(
            uow.Object,
            mapper,
            cache.Object,
            Mock.Of<ILogger<GetInternshipGroupsHandler>>(),
            SuperAdminUser().Object,
            Mock.Of<IMessageService>());

        var result = await handler.Handle(new GetInternshipGroupsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<IOCv2.Application.Common.Models.PaginatedResult<GetInternshipGroupsResponse>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Admin.Users.Queries.GetAdminUsers;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Admin.Users.Queries;

public class GetAdminUsersHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCachedResult_WhenCacheHit()
    {
        var cache = new Mock<ICacheService>();
        var cached = new IOCv2.Application.Common.Models.PaginatedResult<GetAdminUsersResponse>(
            new List<GetAdminUsersResponse> { new() { FullName = "Cached Admin" } }, 1, 1, 10);

        cache.Setup(x => x.GetAsync<IOCv2.Application.Common.Models.PaginatedResult<GetAdminUsersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var handler = new GetAdminUsersHandler(
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<GetAdminUsersHandler>>(),
            cache.Object);

        var result = await handler.Handle(new GetAdminUsersQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle(x => x.FullName == "Cached Admin");
    }

    [Fact]
    public async Task Handle_ReturnsPagedData_WhenCacheMiss()
    {
        var user = new User(Guid.NewGuid(), "SA0001", "a@ioc.com", "Admin One", UserRole.SuperAdmin, "hash");
        var users = new List<User> { user };

        var repo = new Mock<IGenericRepository<User>>();
        repo.Setup(x => x.Query()).Returns(users.AsQueryable().BuildMock());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(repo.Object);

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<IOCv2.Application.Common.Models.PaginatedResult<GetAdminUsersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IOCv2.Application.Common.Models.PaginatedResult<GetAdminUsersResponse>?)null);

        var cfg = new MapperConfiguration(cfg => cfg.CreateMap<User, GetAdminUsersResponse>());
        var mapper = cfg.CreateMapper();

        var handler = new GetAdminUsersHandler(
            uow.Object,
            mapper,
            Mock.Of<ILogger<GetAdminUsersHandler>>(),
            cache.Object);

        var result = await handler.Handle(new GetAdminUsersQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<IOCv2.Application.Common.Models.PaginatedResult<GetAdminUsersResponse>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

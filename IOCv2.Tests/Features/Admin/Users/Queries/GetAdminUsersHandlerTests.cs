using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Admin.UserManagement.Queries.GetUsers;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Admin.UserManagement.Queries;

public class GetUsersHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCachedResult_WhenCacheHit()
    {
        var cache = new Mock<ICacheService>();
        var cached = new IOCv2.Application.Common.Models.PaginatedResult<GetUsersResponse>(
            new List<GetUsersResponse> { new() { FullName = "Cached Admin" } }, 1, 1, 10);

        cache.Setup(x => x.GetAsync<IOCv2.Application.Common.Models.PaginatedResult<GetUsersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var handler = new GetUsersHandler(
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<GetUsersHandler>>(),
            cache.Object,
            Mock.Of<ICurrentUserService>());

        var result = await handler.Handle(new GetUsersQuery(), CancellationToken.None);

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
        cache.Setup(x => x.GetAsync<IOCv2.Application.Common.Models.PaginatedResult<GetUsersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IOCv2.Application.Common.Models.PaginatedResult<GetUsersResponse>?)null);

        var cfg = new MapperConfiguration(cfg => cfg.CreateMap<User, GetUsersResponse>());
        var mapper = cfg.CreateMapper();

        var handler = new GetUsersHandler(
            uow.Object,
            mapper,
            Mock.Of<ILogger<GetUsersHandler>>(),
            cache.Object,
            Mock.Of<ICurrentUserService>());

        var result = await handler.Handle(new GetUsersQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<IOCv2.Application.Common.Models.PaginatedResult<GetUsersResponse>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

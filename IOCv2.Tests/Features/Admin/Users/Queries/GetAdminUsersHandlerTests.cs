using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Admin.UserManagement.Queries.GetUsers;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using System.Reflection;

namespace IOCv2.Tests.Features.Admin.Users.Queries;

public class GetUsersHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCachedResult_WhenCacheHit()
    {
        var cache = new Mock<ICacheService>();
        var cached = new Application.Common.Models.PaginatedResult<GetUsersResponse>(
            new List<GetUsersResponse> { new() { FullName = "Cached Admin" } }, 1, 1, 10);

        cache.Setup(x => x.GetAsync<Application.Common.Models.PaginatedResult<GetUsersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var handler = new GetUsersHandler(
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<GetUsersHandler>>(),
            cache.Object,
            GetMockCurrentUserService().Object);

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
        cache.Setup(x => x.GetAsync<Application.Common.Models.PaginatedResult<GetUsersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.Common.Models.PaginatedResult<GetUsersResponse>?)null);

        var mapperCfg = new MapperConfiguration(c => c.CreateMap<User, GetUsersResponse>());
        var mapper = mapperCfg.CreateMapper();

        var handler = new GetUsersHandler(
            uow.Object,
            mapper,
            Mock.Of<ILogger<GetUsersHandler>>(),
            cache.Object,
            GetMockCurrentUserService().Object);

        var result = await handler.Handle(new GetUsersQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        cache.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Application.Common.Models.PaginatedResult<GetUsersResponse>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsHrAndMentorsInSameEnterprise_WhenAuditorIsHr()
    {
        var enterpriseId = Guid.NewGuid();

        var sameEnterpriseMentor = new User(Guid.NewGuid(), "ME0001", "mentor@ioc.com", "Mentor One", UserRole.Mentor, "hash");
        SetEnterpriseUser(sameEnterpriseMentor, enterpriseId);

        var sameEnterpriseHr = new User(Guid.NewGuid(), "HR0001", "hr@ioc.com", "HR One", UserRole.HR, "hash");
        SetEnterpriseUser(sameEnterpriseHr, enterpriseId);

        var otherEnterpriseMentor = new User(Guid.NewGuid(), "ME0002", "mentor2@ioc.com", "Mentor Two", UserRole.Mentor, "hash");
        SetEnterpriseUser(otherEnterpriseMentor, Guid.NewGuid());

        var users = new List<User> { sameEnterpriseMentor, sameEnterpriseHr, otherEnterpriseMentor };

        var repo = new Mock<IGenericRepository<User>>();
        repo.Setup(x => x.Query()).Returns(users.AsQueryable().BuildMock());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(repo.Object);

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<Application.Common.Models.PaginatedResult<GetUsersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.Common.Models.PaginatedResult<GetUsersResponse>?)null);

        var mapperCfg = new MapperConfiguration(c => c.CreateMap<User, GetUsersResponse>());
        var mapper = mapperCfg.CreateMapper();

        var handler = new GetUsersHandler(
            uow.Object,
            mapper,
            Mock.Of<ILogger<GetUsersHandler>>(),
            cache.Object,
            GetMockCurrentUserService(role: "HR", unitId: enterpriseId.ToString()).Object);

        var result = await handler.Handle(new GetUsersQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(2);
        result.Data.Items.Should().Contain(x => x.UserId == sameEnterpriseMentor.UserId);
        result.Data.Items.Should().Contain(x => x.UserId == sameEnterpriseHr.UserId);
    }

    [Fact]
    public async Task Handle_ReturnsSuccess_WhenHrFiltersByHrRole()
    {
        var repo = new Mock<IGenericRepository<User>>();
        repo.Setup(x => x.Query()).Returns(new List<User>().AsQueryable().BuildMock());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(repo.Object);

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<Application.Common.Models.PaginatedResult<GetUsersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.Common.Models.PaginatedResult<GetUsersResponse>?)null);

        var mapperCfg = new MapperConfiguration(c => c.CreateMap<User, GetUsersResponse>());
        var mapper = mapperCfg.CreateMapper();

        var handler = new GetUsersHandler(
            uow.Object,
            mapper,
            Mock.Of<ILogger<GetUsersHandler>>(),
            cache.Object,
            GetMockCurrentUserService(role: "HR").Object);

        var result = await handler.Handle(new GetUsersQuery { Role = UserRole.HR, PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    private Mock<ICurrentUserService> GetMockCurrentUserService(string role = "SuperAdmin", string? unitId = null)
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mock.Setup(x => x.Role).Returns(role);
        mock.Setup(x => x.UnitId).Returns(unitId ?? Guid.NewGuid().ToString());
        return mock;
    }

    private static void SetEnterpriseUser(User user, Guid enterpriseId)
    {
        var enterpriseUser = new EnterpriseUser
        {
            EnterpriseId = enterpriseId,
            UserId = user.UserId,
            Enterprise = new Enterprise { EnterpriseId = enterpriseId, Name = "Test Enterprise" },
            User = user
        };

        typeof(User).GetProperty(nameof(User.EnterpriseUser), BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(user, enterpriseUser);
    }
}

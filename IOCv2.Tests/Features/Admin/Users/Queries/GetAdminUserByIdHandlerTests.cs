using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Admin.UserManagement.Queries.GetUserById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Admin.Users.Queries;

public class GetUserByIdHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsCached_WhenCacheHit()
    {
        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<GetUserByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetUserByIdResponse { FullName = "Cached User" });

        var handler = new GetUserByIdHandler(
            Mock.Of<IUnitOfWork>(),
            Mock.Of<IMapper>(),
            Mock.Of<IMessageService>(),
            cache.Object,
            GetMockCurrentUserService().Object,
            Mock.Of<ILogger<GetUserByIdHandler>>());

        var result = await handler.Handle(new GetUserByIdQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FullName.Should().Be("Cached User");
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenUserMissing()
    {
        var repo = new Mock<IGenericRepository<User>>();
        repo.Setup(x => x.Query()).Returns(new List<User>().AsQueryable().BuildMock());

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(repo.Object);

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.GetAsync<GetUserByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetUserByIdResponse?)null);

        var message = new Mock<IMessageService>();
        message.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("User not found");

        var cfg = new MapperConfiguration(cfg => cfg.CreateMap<User, GetUserByIdResponse>());
        var mapper = cfg.CreateMapper();

        var handler = new GetUserByIdHandler(
            uow.Object,
            mapper,
            message.Object,
            cache.Object,
            GetMockCurrentUserService().Object,
            Mock.Of<ILogger<GetUserByIdHandler>>());

        var result = await handler.Handle(new GetUserByIdQuery { UserId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(Application.Common.Models.ResultErrorType.NotFound);
    }

    private Mock<ICurrentUserService> GetMockCurrentUserService()
    {
        var mock = new Mock<ICurrentUserService>();
        mock.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mock.Setup(x => x.Role).Returns("SuperAdmin");
        mock.Setup(x => x.UnitId).Returns(Guid.NewGuid().ToString());
        return mock;
    }
}

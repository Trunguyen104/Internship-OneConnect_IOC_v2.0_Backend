using FluentAssertions;
using IOCv2.Application.Features.Authentication.Commands.RefreshTokens;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Authentication;

public class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSuccess_ForValidRefreshToken()
    {
        var user = new User(Guid.NewGuid(), "SA0001", "a@b.com", "Admin", UserRole.SuperAdmin, "hash");
        var refresh = new RefreshToken
        {
            Token = "old-token",
            UserId = user.UserId,
            User = user,
            Expires = DateTime.UtcNow.AddDays(3),
            CreatedAt = DateTime.UtcNow
        };

        var refreshRepo = new Mock<IGenericRepository<RefreshToken>>();
        refreshRepo.Setup(x => x.Query()).Returns(new List<RefreshToken> { refresh }.AsQueryable().BuildMock());
        refreshRepo.Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        refreshRepo.Setup(x => x.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).ReturnsAsync((RefreshToken t, CancellationToken _) => t);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<RefreshToken>()).Returns(refreshRepo.Object);
        uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var tokenService = new Mock<ITokenService>();
        tokenService.Setup(x => x.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
        tokenService.Setup(x => x.GenerateRefreshToken()).Returns("new-refresh-token");
        tokenService.Setup(x => x.GetTokenExpirationSeconds()).Returns(3600);
        tokenService.Setup(x => x.GetRefreshTokenExpirationDays()).Returns(7);

        var handler = new RefreshTokenHandler(
            uow.Object,
            tokenService.Object,
            Mock.Of<IMessageService>(),
            Mock.Of<ILogger<RefreshTokenHandler>>());

        var result = await handler.Handle(new RefreshTokenCommand("old-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().Be("access-token");
        result.Data.RefreshToken.Should().Be("new-refresh-token");
    }
}

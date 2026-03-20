using FluentAssertions;
using IOCv2.Application.Features.Authentication.Commands.RevokeToken;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Authentication;

public class RevokeTokenHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsSuccess_WhenTokenExists()
    {
        var token = new RefreshToken { Token = "revoke-me", UserId = Guid.NewGuid(), Expires = DateTime.UtcNow.AddDays(1) };

        var repo = new Mock<IGenericRepository<RefreshToken>>();
        repo.Setup(x => x.Query()).Returns(new List<RefreshToken> { token }.AsQueryable().BuildMock());
        repo.Setup(x => x.DeleteAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<RefreshToken>()).Returns(repo.Object);
        uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RevokeTokenHandler(
            uow.Object,
            Mock.Of<IMessageService>(),
            Mock.Of<ILogger<RevokeTokenHandler>>());

        var result = await handler.Handle(new RevokeTokenCommand { RefreshToken = "revoke-me" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
    }
}

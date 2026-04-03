using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Admin.UserManagement.Commands.DeleteUser;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Admin.Users.Commands;

public class DeleteUserHandlerTests
{
    [Fact]
    public async Task Handle_DeletesUserAndClearsCache()
    {
        var user = new User(Guid.NewGuid(), "SA0003", "user@ioc.com", "User B", UserRole.SuperAdmin, "hash");
        var otherSuperAdmin = new User(Guid.NewGuid(), "SA0004", "other@ioc.com", "User C", UserRole.SuperAdmin, "hash");

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo.Setup(x => x.Query()).Returns(new List<User> { user, otherSuperAdmin }.AsQueryable().BuildMock());
        userRepo.Setup(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var auditRepo = new Mock<IGenericRepository<AuditLog>>();
        auditRepo.Setup(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.Repository<AuditLog>()).Returns(auditRepo.Object);
        
        var refreshToken = new RefreshToken
        {
            RefreshTokenId = Guid.NewGuid(),
            Token = "rt",
            UserId = user.UserId,
            Expires = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
        var refreshRepo = new Mock<IGenericRepository<RefreshToken>>();
        refreshRepo.Setup(x => x.Query()).Returns(new List<RefreshToken> { refreshToken }.AsQueryable().BuildMock());
        refreshRepo.Setup(x => x.UpdateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.Repository<RefreshToken>()).Returns(refreshRepo.Object);

        uow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        currentUser.Setup(x => x.Role).Returns("SuperAdmin");
        currentUser.Setup(x => x.UnitId).Returns(Guid.NewGuid().ToString());

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<DeleteUserResponse>(It.IsAny<User>()))
            .Returns((User src) => new DeleteUserResponse { UserId = src.UserId, FullName = src.FullName, Email = src.Email });

        var handler = new DeleteUserHandler(
            uow.Object,
            mapper.Object,
            currentUser.Object,
            Mock.Of<IMessageService>(),
            cache.Object,
            Mock.Of<ILogger<DeleteUserHandler>>());

        var result = await handler.Handle(new DeleteUserCommand { UserId = user.UserId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cache.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

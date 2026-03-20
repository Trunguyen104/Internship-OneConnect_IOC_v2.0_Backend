using FluentAssertions;
using IOCv2.Application.Features.Admin.Users.Commands.ResetUserPassword;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Admin.Users.Commands;

public class ResetUserPasswordHandlerTests
{
    [Fact]
    public async Task Handle_ResetsPasswordAndInvalidatesCache()
    {
        var user = new User(Guid.NewGuid(), "SA0099", "sa@ioc.com", "Super A", UserRole.SuperAdmin, "old-hash");

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        userRepo.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var refreshRepo = new Mock<IGenericRepository<RefreshToken>>();
        refreshRepo.Setup(x => x.Query()).Returns(new List<RefreshToken>().AsQueryable().BuildMock());

        var auditRepo = new Mock<IGenericRepository<AuditLog>>();
        auditRepo.Setup(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.Repository<RefreshToken>()).Returns(refreshRepo.Object);
        uow.Setup(x => x.Repository<AuditLog>()).Returns(auditRepo.Object);
        uow.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        uow.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        uow.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());

        var passwordService = new Mock<IPasswordService>();
        passwordService.Setup(x => x.GenerateRandomPassword()).Returns("NewPass@123");
        passwordService.Setup(x => x.HashPassword(It.IsAny<string>())).Returns("new-hash");

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        cache.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new ResetUserPasswordHandler(
            uow.Object,
            passwordService.Object,
            Mock.Of<IBackgroundEmailSender>(),
            currentUser.Object,
            Mock.Of<IMessageService>(),
            cache.Object,
            Mock.Of<ILogger<ResetUserPasswordHandler>>());

        var result = await handler.Handle(new ResetUserPasswordCommand { UserId = user.UserId, Reason = "Security" }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Admin.UserManagement.Commands.ToggleUserStatus;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Admin.UserManagement.Commands;

public class ToggleUserStatusHandlerTests
{
    [Fact]
    public async Task Handle_UpdatesStatusAndInvalidatesCache()
    {
        var user = new User(Guid.NewGuid(), "SA0002", "user@ioc.com", "User A", UserRole.SuperAdmin, "hash");

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo.Setup(x => x.Query()).Returns(new List<User> { user }.AsQueryable().BuildMock());
        userRepo.Setup(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var auditRepo = new Mock<IGenericRepository<AuditLog>>();
        auditRepo.Setup(x => x.AddAsync(It.IsAny<AuditLog>(), It.IsAny<CancellationToken>())).ReturnsAsync((AuditLog a, CancellationToken _) => a);

        var uow = new Mock<IUnitOfWork>();
        uow.Setup(x => x.Repository<User>()).Returns(userRepo.Object);
        uow.Setup(x => x.Repository<AuditLog>()).Returns(auditRepo.Object);
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
        mapper.Setup(x => x.Map<ToggleUserStatusResponse>(It.IsAny<User>()))
            .Returns((User src) => new ToggleUserStatusResponse { UserId = src.UserId, Status = src.Status });

        var handler = new ToggleUserStatusHandler(
            uow.Object,
            mapper.Object,
            currentUser.Object,
            Mock.Of<IMessageService>(),
            cache.Object,
            Mock.Of<ILogger<ToggleUserStatusHandler>>());

        var result = await handler.Handle(new ToggleUserStatusCommand { UserId = user.UserId, NewStatus = UserStatus.Inactive }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(UserStatus.Inactive);
        cache.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

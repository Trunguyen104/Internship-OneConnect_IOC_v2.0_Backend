using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;

namespace IOCv2.Tests.Features.Admin.Users.Commands;

public class DeleteAdminUserHandlerTests
{
    [Fact]
    public async Task Handle_DeletesUserAndClearsCache()
    {
        var user = new User(Guid.NewGuid(), "SA0003", "user@ioc.com", "User B", UserRole.SuperAdmin, "hash");

        var userRepo = new Mock<IGenericRepository<User>>();
        userRepo.Setup(x => x.GetByIdAsync(user.UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        userRepo.Setup(x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

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

        var cache = new Mock<ICacheService>();
        cache.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        cache.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mapper = new Mock<IMapper>();
        mapper.Setup(x => x.Map<DeleteAdminUserResponse>(It.IsAny<User>()))
            .Returns((User src) => new DeleteAdminUserResponse { UserId = src.UserId, FullName = src.FullName, Email = src.Email });

        var handler = new DeleteAdminUserHandler(
            uow.Object,
            mapper.Object,
            currentUser.Object,
            Mock.Of<IMessageService>(),
            cache.Object,
            Mock.Of<ILogger<DeleteAdminUserHandler>>());

        var result = await handler.Handle(new DeleteAdminUserCommand { UserId = user.UserId }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        cache.Verify(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Terms.Queries.GetTermById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace IOCv2.Tests.Features.Terms.Queries;

public class GetTermByIdHandlerTests
{
    [Fact]
    public async Task Handle_EnterpriseUserMappingNotFound_ReturnsForbidden()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockMessageService = new Mock<IMessageService>();
        var mockLogger = new Mock<ILogger<GetTermByIdHandler>>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCacheService = new Mock<ICacheService>();
        var mockEnterpriseUserRepo = new Mock<IGenericRepository<EnterpriseUser>>();

        mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUserService.Setup(x => x.Role).Returns("HR");
        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        mockEnterpriseUserRepo.Setup(x => x.Query())
            .Returns(new List<EnterpriseUser>().AsQueryable().BuildMock());

        mockUnitOfWork.Setup(x => x.Repository<EnterpriseUser>())
            .Returns(mockEnterpriseUserRepo.Object);

        var handler = new GetTermByIdHandler(
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockMessageService.Object,
            mockLogger.Object,
            mockCurrentUserService.Object,
            mockCacheService.Object);

        var result = await handler.Handle(new GetTermByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }
}



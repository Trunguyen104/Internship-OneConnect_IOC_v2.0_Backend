using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Terms.Queries.GetTerms;
using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IOCv2.Tests.Features.Terms.Queries;

public class GetTermsHandlerTests
{
    [Fact]
    public async Task Handle_UserWithoutSupportedRole_ReturnsForbidden()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockMessageService = new Mock<IMessageService>();
        var mockLogger = new Mock<ILogger<GetTermsHandler>>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCacheService = new Mock<ICacheService>();

        mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUserService.Setup(x => x.Role).Returns("Student");
        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        var handler = new GetTermsHandler(
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockMessageService.Object,
            mockLogger.Object,
            mockCurrentUserService.Object,
            mockCacheService.Object);

        var result = await handler.Handle(new GetTermsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Forbidden);
    }
}


using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhases;
using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IOCv2.Tests.Features.InternshipPhases.Queries;

public class GetInternshipPhasesHandlerTests
{
    [Fact]
    public async Task Handle_InvalidUserId_ForEnterpriseScopedRole_ReturnsUnauthorized()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockMessageService = new Mock<IMessageService>();
        var mockCacheService = new Mock<ICacheService>();
        var mockLogger = new Mock<ILogger<GetInternshipPhasesHandler>>();

        mockCurrentUserService.Setup(x => x.Role).Returns("HR");
        mockCurrentUserService.Setup(x => x.UserId).Returns("invalid-guid");
        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        var handler = new GetInternshipPhasesHandler(
            mockUnitOfWork.Object,
            mockCurrentUserService.Object,
            mockMessageService.Object,
            mockCacheService.Object,
            mockLogger.Object);

        var result = await handler.Handle(new GetInternshipPhasesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }
}


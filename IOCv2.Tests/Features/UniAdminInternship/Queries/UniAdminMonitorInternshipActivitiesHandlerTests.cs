using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentEvaluations;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbookTotal;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbookWeekly;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentViolations;
using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace IOCv2.Tests.Features.UniAdminInternship.Queries;

public class UniAdminMonitorInternshipActivitiesHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly Mock<ICurrentUserService> _mockCurrentUserService = new();
    private readonly Mock<IMessageService> _mockMessageService = new();

    public UniAdminMonitorInternshipActivitiesHandlerTests()
    {
        _mockCurrentUserService.Setup(x => x.UserId).Returns("invalid-guid");
        _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
    }

    [Fact]
    public async Task LogbookTotal_InvalidUserId_ReturnsUnauthorized()
    {
        var handler = new GetUniAdminStudentLogbookTotalHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            new Mock<ILogger<GetUniAdminStudentLogbookTotalHandler>>().Object);

        var result = await handler.Handle(new GetUniAdminStudentLogbookTotalQuery { StudentId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }

    [Fact]
    public async Task LogbookWeekly_InvalidUserId_ReturnsUnauthorized()
    {
        var handler = new GetUniAdminStudentLogbookWeeklyHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            new Mock<ILogger<GetUniAdminStudentLogbookWeeklyHandler>>().Object);

        var result = await handler.Handle(new GetUniAdminStudentLogbookWeeklyQuery { StudentId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }

    [Fact]
    public async Task Violations_InvalidUserId_ReturnsUnauthorized()
    {
        var handler = new GetUniAdminStudentViolationsHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            new Mock<ILogger<GetUniAdminStudentViolationsHandler>>().Object);

        var result = await handler.Handle(new GetUniAdminStudentViolationsQuery { StudentId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }

    [Fact]
    public async Task Evaluations_InvalidUserId_ReturnsUnauthorized()
    {
        var handler = new GetUniAdminStudentEvaluationsHandler(
            _mockUnitOfWork.Object,
            _mockCurrentUserService.Object,
            _mockMessageService.Object,
            new Mock<ILogger<GetUniAdminStudentEvaluationsHandler>>().Object);

        var result = await handler.Handle(new GetUniAdminStudentEvaluationsQuery { StudentId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }
}


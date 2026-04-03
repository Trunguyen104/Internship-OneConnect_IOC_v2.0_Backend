using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace IOCv2.Tests.Features.Projects.Queries;

public class GetProjectsByInternshipIdHandlerTests
{
    [Fact]
    public async Task Handle_InternshipGroupNotFound_ReturnsNotFound()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockMessageService = new Mock<IMessageService>();
        var mockLogger = new Mock<ILogger<GetProjectsByInternshipIdHandler>>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockInternshipGroupRepo = new Mock<IGenericRepository<InternshipGroup>>();

        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);
        mockInternshipGroupRepo.Setup(x => x.Query())
            .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());
        mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(mockInternshipGroupRepo.Object);

        var handler = new GetProjectsByInternshipIdHandler(
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockMessageService.Object,
            mockLogger.Object,
            mockCurrentUserService.Object);

        var result = await handler.Handle(new GetProjectsByInternshipIdQuery { InternshipId = Guid.NewGuid() }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }
}



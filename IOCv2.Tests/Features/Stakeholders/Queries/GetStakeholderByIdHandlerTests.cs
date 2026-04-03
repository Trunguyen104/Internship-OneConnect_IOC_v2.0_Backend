using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;

namespace IOCv2.Tests.Features.Stakeholders.Queries;

public class GetStakeholderByIdHandlerTests
{
    [Fact]
    public async Task Handle_StakeholderNotFound_ReturnsNotFound()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockMessageService = new Mock<IMessageService>();
        var mockLogger = new Mock<ILogger<GetStakeholderByIdHandler>>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCacheService = new Mock<ICacheService>();
        var mockStakeholderRepo = new Mock<IGenericRepository<Stakeholder>>();

        mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        mockStakeholderRepo.Setup(x => x.Query())
            .Returns(new List<Stakeholder>().AsQueryable().BuildMock());
        mockUnitOfWork.Setup(x => x.Repository<Stakeholder>()).Returns(mockStakeholderRepo.Object);

        var handler = new GetStakeholderByIdHandler(
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockMessageService.Object,
            mockLogger.Object,
            mockCurrentUserService.Object,
            mockCacheService.Object);

        var query = new GetStakeholderByIdQuery { StakeholderId = Guid.NewGuid(), InternshipId = Guid.NewGuid() };

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }
}



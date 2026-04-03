using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace IOCv2.Tests.Features.Stakeholders.Queries;

public class GetStakeholdersHandlerTests
{
    [Fact]
    public async Task Handle_InvalidUserId_ReturnsUnauthorized()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockMessageService = new Mock<IMessageService>();
        var mockLogger = new Mock<ILogger<GetStakeholdersHandler>>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCacheService = new Mock<ICacheService>();
        var mockInternshipGroupRepo = new Mock<IGenericRepository<InternshipGroup>>();

        mockCurrentUserService.Setup(x => x.UserId).Returns("invalid-guid");
        mockCurrentUserService.Setup(x => x.Role).Returns("Student");
        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        mockInternshipGroupRepo
            .Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(mockInternshipGroupRepo.Object);

        var handler = new GetStakeholdersHandler(
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockMessageService.Object,
            mockLogger.Object,
            mockCurrentUserService.Object,
            mockCacheService.Object);

        var query = new GetStakeholdersQuery { InternshipId = Guid.NewGuid() };

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(ResultErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedResult()
    {
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var mockMapper = new Mock<IMapper>();
        var mockMessageService = new Mock<IMessageService>();
        var mockLogger = new Mock<ILogger<GetStakeholdersHandler>>();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        var mockCacheService = new Mock<ICacheService>();
        var mockInternshipGroupRepo = new Mock<IGenericRepository<InternshipGroup>>();

        mockCurrentUserService.Setup(x => x.UserId).Returns(Guid.NewGuid().ToString());
        mockCurrentUserService.Setup(x => x.Role).Returns("SuperAdmin");
        mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns((string key) => key);

        mockInternshipGroupRepo
            .Setup(x => x.ExistsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<InternshipGroup, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cached = PaginatedResult<GetStakeholdersResponse>.Create(
            new List<GetStakeholdersResponse> { new() { Name = "PO", Email = "po@test.com" } },
            1,
            1,
            10);

        mockCacheService
            .Setup(x => x.GetAsync<PaginatedResult<GetStakeholdersResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>()).Returns(mockInternshipGroupRepo.Object);

        var handler = new GetStakeholdersHandler(
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockMessageService.Object,
            mockLogger.Object,
            mockCurrentUserService.Object,
            mockCacheService.Object);

        var query = new GetStakeholdersQuery { InternshipId = Guid.NewGuid() };

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(1);
    }
}



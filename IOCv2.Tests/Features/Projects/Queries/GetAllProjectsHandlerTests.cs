using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Features.Projects.Queries.GetAllProjects;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable;
using MockQueryable.Moq;

namespace IOCv2.Tests.Features.Projects.Queries;

public class GetAllProjectsHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<ILogger<GetAllProjectsHandler>> _logger = new();
    private readonly Mock<IMessageService> _messageService = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IGenericRepository<Project>> _projectRepo = new();

    [Fact]
    public async Task Handle_ReturnsCachedResult_WhenCacheHit()
    {
        var query = new GetAllProjectsQuery { PageNumber = 1, PageSize = 10 };
        var cached = new IOCv2.Application.Common.Models.PaginatedResult<GetAllProjectsResponse>(
            new List<GetAllProjectsResponse> { new() { ProjectName = "Cached" } },
            1,
            1,
            10);

        _cacheService.Setup(x => x.GetAsync<IOCv2.Application.Common.Models.PaginatedResult<GetAllProjectsResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var handler = new GetAllProjectsHandler(
            _unitOfWork.Object,
            _mapper.Object,
            _logger.Object,
            _messageService.Object,
            _cacheService.Object);

        var result = await handler.Handle(query, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle(x => x.ProjectName == "Cached");
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult_WhenCacheMiss()
    {
        var project = Project.Create(Guid.NewGuid(), "P1", "Desc", DateTime.UtcNow, DateTime.UtcNow.AddDays(1));
        var data = new List<Project> { project };
        var mockQuery = data.AsQueryable().BuildMock();

        _cacheService.Setup(x => x.GetAsync<IOCv2.Application.Common.Models.PaginatedResult<GetAllProjectsResponse>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IOCv2.Application.Common.Models.PaginatedResult<GetAllProjectsResponse>?)null);

        _projectRepo.Setup(x => x.Query()).Returns(mockQuery);
        _unitOfWork.Setup(x => x.Repository<Project>()).Returns(_projectRepo.Object);

        var cfg = new MapperConfiguration(c => c.CreateMap<Project, GetAllProjectsResponse>());
        var mapper = cfg.CreateMapper();

        var handler = new GetAllProjectsHandler(
            _unitOfWork.Object,
            mapper,
            _logger.Object,
            _messageService.Object,
            _cacheService.Object);

        var result = await handler.Handle(new GetAllProjectsQuery { PageNumber = 1, PageSize = 10 }, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().ContainSingle();
        _cacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<IOCv2.Application.Common.Models.PaginatedResult<GetAllProjectsResponse>>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

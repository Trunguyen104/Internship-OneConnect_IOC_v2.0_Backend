using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.StakeholderIssues.Queries
{
    public class GetStakeholderIssueByIdHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<GetStakeholderIssueByIdQueryHandler>> _mockLogger;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly GetStakeholderIssueByIdQueryHandler _handler;

        private readonly Guid _issueId = Guid.NewGuid();

        public GetStakeholderIssueByIdHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<GetStakeholderIssueByIdQueryHandler>>();
            _mockCacheService = new Mock<ICacheService>();

            // Provide a real MapperConfiguration so ProjectTo does not throw NullReferenceException.
            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<StakeholderIssue, GetStakeholderIssueByIdResponse>()
                    .ForMember(d => d.StakeholderName, opt => opt.Ignore());
            });
            _mockMapper.Setup(x => x.ConfigurationProvider).Returns(mapperConfig);

            _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>()))
                .Returns((string key) => key);

            _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.CommitTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.RollbackTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(x => x.SaveChangeAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            _mockCacheService.Setup(x => x.RemoveByPatternAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: cache miss
            _mockCacheService.Setup(x => x.GetAsync<GetStakeholderIssueByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetStakeholderIssueByIdResponse?)null);

            // Default: SetAsync does nothing
            _mockCacheService.Setup(x => x.SetAsync(It.IsAny<string>(), It.IsAny<GetStakeholderIssueByIdResponse>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Default: empty query (no issue found) — overridden per test as needed
            _mockUnitOfWork.Setup(x => x.Repository<StakeholderIssue>().Query())
                .Returns(new List<StakeholderIssue>().AsQueryable().BuildMock());

            _handler = new GetStakeholderIssueByIdQueryHandler(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockMessageService.Object,
                _mockLogger.Object,
                _mockCacheService.Object);
        }

        [Fact]
        public async Task Handle_CacheHit_ReturnsFromCache()
        {
            // Arrange
            var cachedResponse = new GetStakeholderIssueByIdResponse
            {
                Id = _issueId,
                Title = "Cached Issue",
                Description = "Cached Description"
            };

            _mockCacheService.Setup(x => x.GetAsync<GetStakeholderIssueByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedResponse);

            var query = new GetStakeholderIssueByIdQuery(_issueId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(_issueId);
            result.Data.Title.Should().Be("Cached Issue");

            // Should not hit the database
            _mockUnitOfWork.Verify(x => x.Repository<StakeholderIssue>().Query(), Times.Never);
        }

        [Fact]
        public async Task Handle_CacheMiss_IssueNotFound_ReturnsNotFound()
        {
            // Arrange: cache miss (already set in constructor default), empty query
            _mockCacheService.Setup(x => x.GetAsync<GetStakeholderIssueByIdResponse>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetStakeholderIssueByIdResponse?)null);

            _mockUnitOfWork.Setup(x => x.Repository<StakeholderIssue>().Query())
                .Returns(new List<StakeholderIssue>().AsQueryable().BuildMock());

            var query = new GetStakeholderIssueByIdQuery(Guid.NewGuid());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorType.Should().Be(ResultErrorType.NotFound);

            // Cache should not be set when not found
            _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<GetStakeholderIssueByIdResponse>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

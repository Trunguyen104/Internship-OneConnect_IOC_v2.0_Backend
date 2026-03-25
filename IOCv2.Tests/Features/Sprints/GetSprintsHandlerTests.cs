using AutoMapper;
using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Sprints.Queries.GetSprints;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Extensions.Mappings;
using Microsoft.EntityFrameworkCore.InMemory;

namespace IOCv2.Tests.Features.Sprints.Queries.GetSprints
{
    public class GetSprintsHandlerTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly IMapper _mapper;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IMessageService> _mockMessageService;
        private readonly Mock<ILogger<GetSprintsHandler>> _mockLogger;
        private readonly GetSprintsHandler _handler;

        private readonly DbContextOptions<TestDbContext> _dbOptions;

        public GetSprintsHandlerTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockCacheService = new Mock<ICacheService>();
            _mockMessageService = new Mock<IMessageService>();
            _mockLogger = new Mock<ILogger<GetSprintsHandler>>();

            var configuration = new MapperConfiguration(cfg =>
                cfg.AddProfile<MappingProfile>());
            _mapper = configuration.CreateMapper();

            _dbOptions = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _handler = new GetSprintsHandler(
                _mockUnitOfWork.Object,
                _mapper,
                _mockCacheService.Object,
                _mockMessageService.Object,
                _mockLogger.Object);
        }

        private void SetupRepository(List<Sprint> data)
        {
            var context = new TestDbContext(_dbOptions);
            context.Sprints.AddRange(data);
            context.SaveChanges();

            _mockUnitOfWork.Setup(x => x.Repository<Sprint>().Query())
                .Returns(context.Sprints);
        }

        [Fact]
        public async Task Handle_CacheMiss_ShouldQueryDatabaseAndCacheResult()
        {
            var projectId = Guid.NewGuid();

            var query = new GetSprintsQuery(
                projectId,
                null,
                new PaginationParams { PageIndex = 1, PageSize = 10 });

            var data = new List<Sprint>
            {
                new Sprint(projectId, "Sprint 1", "Goal 1"),
                new Sprint(projectId, "Sprint 2", "Goal 2")
            };

            SetupRepository(data);

            _mockCacheService.Setup(x => x.GetAsync<PaginatedResult<GetSprintsResponse>>(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PaginatedResult<GetSprintsResponse>?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Data!.Items.Should().HaveCount(2);
            result.Data.TotalCount.Should().Be(2);

            _mockCacheService.Verify(x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PaginatedResult<GetSprintsResponse>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }

        public DbSet<Sprint> Sprints { get; set; }
        public DbSet<InternshipPhase> InternshipPhases { get; set; }
        public DbSet<SprintWorkItem> SprintWorkItems { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<InternshipGroup> InternshipGroups { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InternshipPhase>().HasKey(p => p.PhaseId);
            modelBuilder.Entity<Sprint>().HasKey(s => s.SprintId);
            modelBuilder.Entity<SprintWorkItem>().HasKey(sw => new { sw.SprintId, sw.WorkItemId });
            modelBuilder.Entity<Project>().HasKey(p => p.ProjectId);
            modelBuilder.Entity<InternshipGroup>().HasKey(g => g.InternshipId);
            base.OnModelCreating(modelBuilder);
        }
    }
}
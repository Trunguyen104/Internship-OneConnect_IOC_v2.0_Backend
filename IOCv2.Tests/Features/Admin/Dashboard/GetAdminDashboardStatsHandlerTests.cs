using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Admin.Dashboard.Queries.GetAdminDashboardStats;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.Admin.Dashboard;

public class GetAdminDashboardStatsHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly GetAdminDashboardStatsHandler _handler;

    public GetAdminDashboardStatsHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new GetAdminDashboardStatsHandler(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsSuccessWithPopulateddata()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Mock counts
        _mockUnitOfWork.Setup(x => x.Repository<User>().CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), cancellationToken))
            .ReturnsAsync(100);
        _mockUnitOfWork.Setup(x => x.Repository<University>().CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<University, bool>>>(), cancellationToken))
            .ReturnsAsync(5);
        _mockUnitOfWork.Setup(x => x.Repository<Enterprise>().CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Enterprise, bool>>>(), cancellationToken))
            .ReturnsAsync(10);
        _mockUnitOfWork.Setup(x => x.Repository<Job>().CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Job, bool>>>(), cancellationToken))
            .ReturnsAsync(50);
        _mockUnitOfWork.Setup(x => x.Repository<Student>().CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Student, bool>>>(), cancellationToken))
            .ReturnsAsync(300);
        _mockUnitOfWork.Setup(x => x.Repository<Term>().CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Term, bool>>>(), cancellationToken))
            .ReturnsAsync(2);
        _mockUnitOfWork.Setup(x => x.Repository<InternshipApplication>().CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<InternshipApplication, bool>>>(), cancellationToken))
            .ReturnsAsync(15);

        // Mock AuditLogs (Important for "mock data" requirement)
        var user = new User(Guid.NewGuid(), "U001", "actor@test.com", "Action Actor", UserRole.SuperAdmin, "hash");
        var auditLogs = new List<AuditLog>
        {
            new AuditLog { 
                AuditLogId = Guid.NewGuid(), 
                Action = AuditAction.Create, 
                EntityType = "Project", 
                PerformedBy = user,
                CreatedAt = DateTime.UtcNow 
            }
        }.AsQueryable().BuildMock();

        _mockUnitOfWork.Setup(x => x.Repository<AuditLog>().Query()).Returns(auditLogs);

        // Act
        var result = await _handler.Handle(new GetAdminDashboardStatsQuery(), cancellationToken);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.TotalUsers.Should().Be(100);
        result.Data.TotalUniversities.Should().Be(5);
        result.Data.RecentActivities.Should().NotBeEmpty();
        result.Data.SystemHealth.Should().Contain(h => h.Label == "Database Status" && h.Status == "good");
    }
}

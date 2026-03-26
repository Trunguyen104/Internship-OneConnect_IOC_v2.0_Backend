using FluentAssertions;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.InternshipGroups.Queries.GetDashboard;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using MockQueryable.Moq;
using MockQueryable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace IOCv2.Tests.Features.InternshipGroups;

public class GetInternshipGroupDashboardHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<GetInternshipGroupDashboardHandler>> _mockLogger;
    private readonly Mock<IMessageService> _mockMessageService;
    private readonly GetInternshipGroupDashboardHandler _handler;

    public GetInternshipGroupDashboardHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<GetInternshipGroupDashboardHandler>>();
        _mockMessageService = new Mock<IMessageService>();
        _handler = new GetInternshipGroupDashboardHandler(_mockUnitOfWork.Object, _mockLogger.Object, _mockMessageService.Object);
    }

    [Fact]
    public async Task Handle_ExistingGroup_ShouldReturnSuccessWithData()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var group = InternshipGroup.Create(Guid.NewGuid(), "Test Group", startDate: DateTime.UtcNow.AddDays(-7), endDate: DateTime.UtcNow.AddDays(7));
        
        // Reflection to set Private InternshipId if needed, but Create sets it.
        // Actually InternshipGroup.Create sets InternshipId = Guid.NewGuid(), so I need to capture it.
        var internshipId = group.InternshipId;
        var query = new GetInternshipGroupDashboardQuery(internshipId);

        var project = Project.Create(internshipId, "Test Project", "Desc", "PRJ-TEST_TST_1", "IT", "Requirements");
        var workItem1 = new WorkItem { 
            Title = "Task 1", 
            Status = WorkItemStatus.Done, 
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            UpdatedAt = DateTime.UtcNow
        };
        var workItem2 = new WorkItem { 
            Title = "Task 2", 
            Status = WorkItemStatus.InProgress, 
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)) 
        };
        project.WorkItems.Add(workItem1);
        project.WorkItems.Add(workItem2);
        group.Projects.Add(project);

        _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
            .Returns(new List<InternshipGroup> { group }.AsQueryable().BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Summary.TotalTasks.Should().Be(2);
        result.Data.Summary.Done.Should().Be(1);
        result.Data.Summary.InProgress.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NonExistingGroup_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetInternshipGroupDashboardQuery(Guid.NewGuid());
        _mockMessageService.Setup(x => x.GetMessage(It.IsAny<string>())).Returns("Internship group not found.");

        _mockUnitOfWork.Setup(x => x.Repository<InternshipGroup>().Query())
            .Returns(new List<InternshipGroup>().AsQueryable().BuildMock());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Internship group not found.");
    }
}

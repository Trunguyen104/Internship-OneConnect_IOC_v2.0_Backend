using FluentAssertions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.WorkItems.Queries.GetWorkItems;
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

namespace IOCv2.Tests.Features.WorkItems.Queries;

public class GetWorkItemsHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly GetWorkItemsHandler _handler;

    public GetWorkItemsHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _handler = new GetWorkItemsHandler(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReturnsPaginatedWorkItems()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        
        // Use static factory for User
        var user = User.Create(Guid.NewGuid(), "actor@test.com");
        user.UpdateProfile("John Doe", null, "url", null, null, null);

        // Assignee is a Student in WorkItem
        var student = new Student { StudentId = assigneeId, User = user };

        var now = DateTime.UtcNow;
        var workItems = new List<WorkItem>
        {
            new WorkItem { 
                WorkItemId = Guid.NewGuid(), 
                Title = "Task 1", 
                ProjectId = projectId, 
                Type = WorkItemType.Task, 
                Status = WorkItemStatus.Todo,
                AssigneeId = assigneeId,
                Assignee = student,
                CreatedAt = now.AddMinutes(1) // Later -> should be first if desc
            },
            new WorkItem { 
                WorkItemId = Guid.NewGuid(), 
                Title = "Bug 1", 
                ProjectId = projectId, 
                Type = WorkItemType.Subtask, 
                Status = WorkItemStatus.InProgress,
                CreatedAt = now
            }
        }.AsQueryable().BuildMock();

        _mockUnitOfWork.Setup(x => x.Repository<WorkItem>().Query()).Returns(workItems);

        var query = new GetWorkItemsQuery
        {
            ProjectId = projectId,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().HaveCount(2);
        
        // Find the one with Title "Task 1" to be safe
        var task1 = result.Data.Items.FirstOrDefault(i => i.Title == "Task 1");
        task1.Should().NotBeNull();
        task1!.AssigneeName.Should().Be("John Doe");
    }

    [Fact]
    public async Task Handle_WithFilters_ReturnsFilteredWorkItems()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var workItems = new List<WorkItem>
        {
            new WorkItem { WorkItemId = Guid.NewGuid(), Title = "Task 1", ProjectId = projectId, Status = WorkItemStatus.Done, Type = WorkItemType.Task },
            new WorkItem { WorkItemId = Guid.NewGuid(), Title = "Task 2", ProjectId = projectId, Status = WorkItemStatus.InProgress, Type = WorkItemType.Task }
        }.AsQueryable().BuildMock();

        _mockUnitOfWork.Setup(x => x.Repository<WorkItem>().Query()).Returns(workItems);

        var query = new GetWorkItemsQuery
        {
            ProjectId = projectId,
            Status = WorkItemStatus.Done,
            PageNumber = 1,
            PageSize = 10
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Items.Should().HaveCount(1);
        result.Data.Items.First().Title.Should().Be("Task 1");
    }
}

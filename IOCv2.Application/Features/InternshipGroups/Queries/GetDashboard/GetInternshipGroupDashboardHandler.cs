using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetDashboard;

/// <summary>
/// Handler for GetInternshipGroupDashboardQuery.
/// </summary>
public class GetInternshipGroupDashboardHandler : IRequestHandler<GetInternshipGroupDashboardQuery, Result<GetInternshipGroupDashboardResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetInternshipGroupDashboardHandler> _logger;
    private readonly IMessageService _messageService;

    public GetInternshipGroupDashboardHandler(IUnitOfWork unitOfWork, ILogger<GetInternshipGroupDashboardHandler> logger, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _messageService = messageService;
    }

    public async Task<Result<GetInternshipGroupDashboardResponse>> Handle(GetInternshipGroupDashboardQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDashboardQuery), request.InternshipId);

        var dashboardData = await _unitOfWork.Repository<InternshipGroup>()
            .Query()
            .AsNoTracking()
            .Where(g => g.InternshipId == request.InternshipId)
            .Select(g => new
            {
                g.StartDate,
                g.EndDate,
                WorkItems = g.Projects.SelectMany(p => p.WorkItems).Select(w => new
                {
                    w.Status,
                    w.DueDate,
                    w.UpdatedAt,
                    AssigneeName = w.Assignee != null && w.Assignee.User != null ? w.Assignee.User.FullName : null
                }).ToList(),
                Logbooks = g.Logbooks.Select(l => new { l.Status }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (dashboardData == null)
        {
            _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDashboardNotFound), request.InternshipId);
            return Result<GetInternshipGroupDashboardResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
        }

        var workItems = dashboardData.WorkItems;
        var logbooks = dashboardData.Logbooks;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var response = new GetInternshipGroupDashboardResponse();

        // 1. Summary
        response.Summary = new DashboardSummaryDto
        {
            TotalTasks = workItems.Count(w => w.Status != WorkItemStatus.Cancelled),
            InProgress = workItems.Count(w => w.Status == WorkItemStatus.Todo || w.Status == WorkItemStatus.InProgress || w.Status == WorkItemStatus.Review),
            Done = workItems.Count(w => w.Status == WorkItemStatus.Done),
            Overdue = workItems.Count(w => w.Status != WorkItemStatus.Done && w.Status != WorkItemStatus.Cancelled && w.DueDate.HasValue && w.DueDate.Value < today)
        };

        // 2. Burndown Calculation
        var endDate = dashboardData.EndDate.HasValue ? DateOnly.FromDateTime(dashboardData.EndDate.Value) : today;
        var startDate = dashboardData.StartDate.HasValue ? DateOnly.FromDateTime(dashboardData.StartDate.Value) : endDate.AddDays(-14);
        
        // Limit range to 30 days to avoid performance issues
        if ((endDate.DayNumber - startDate.DayNumber) > 30) startDate = endDate.AddDays(-30);

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var remaining = workItems
                .Where(w => w.Status != WorkItemStatus.Cancelled)
                .Count(w => 
                    (w.Status != WorkItemStatus.Done) || 
                    (w.Status == WorkItemStatus.Done && w.UpdatedAt.HasValue && DateOnly.FromDateTime(w.UpdatedAt.Value) > date));
            
            response.Burndown.Add(new BurndownDto
            {
                Date = date.ToString("yyyy-MM-dd"),
                Remaining = remaining
            });
        }

        // 3. Completion Ratio
        response.CompletionRatio = new CompletionRatioDto
        {
            OnTime = workItems.Count(w => w.Status == WorkItemStatus.Done && (!w.DueDate.HasValue || (w.UpdatedAt.HasValue && DateOnly.FromDateTime(w.UpdatedAt.Value) <= w.DueDate.Value))),
            Overdue = response.Summary.Overdue
        };

        // 4. Task Status Distribution
        response.TaskStatusDistribution = Enum.GetValues<WorkItemStatus>()
            .Select(s => new TaskStatusDistributionDto
            {
                Status = s.ToString(),
                Count = workItems.Count(w => w.Status == s)
            }).ToList();

        // 5. Workload By Person
        var unassignedLabel = _messageService.GetMessage(MessageKeys.InternshipGroups.Unassigned);
        response.WorkloadByPerson = workItems
            .GroupBy(w => w.AssigneeName ?? unassignedLabel)
            .Select(g => new WorkloadDto
            {
                Name = g.Key,
                Count = g.Count()
            }).OrderByDescending(x => x.Count).ToList();

        // 6. Student Violations
        var lateLogbookLabel = _messageService.GetMessage(MessageKeys.InternshipGroups.LateLogbookSubmission);
        response.StudentViolations = logbooks
            .Where(l => l.Status == LogbookStatus.LATE)
            .GroupBy(l => lateLogbookLabel)
            .Select(g => new ViolationDto
            {
                Type = g.Key,
                Count = g.Count()
            }).ToList();

        _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDashboardGenerated), request.InternshipId, response.Summary.TotalTasks);

        return Result<GetInternshipGroupDashboardResponse>.Success(response);
    }
}

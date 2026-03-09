using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToBacklog;

public class MoveWorkItemToBacklogHandler
    : IRequestHandler<MoveWorkItemToBacklogCommand, Result<MoveWorkItemToBacklogResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<MoveWorkItemToBacklogHandler> _logger;

    public MoveWorkItemToBacklogHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<MoveWorkItemToBacklogHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<MoveWorkItemToBacklogResponse>> Handle(
        MoveWorkItemToBacklogCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moving work item {WorkItemId} to backlog for project {ProjectId}", request.WorkItemId, request.ProjectId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var workItem = await _unitOfWork.Repository<WorkItem>()
            .Query()
            .FirstOrDefaultAsync(w => w.WorkItemId == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);

        if (workItem is null)
            return Result<MoveWorkItemToBacklogResponse>.Failure(
                _messageService.GetMessage(MessageKeys.WorkItem.NotFound), ResultErrorType.NotFound);

        // Remove from sprint (if currently in one)
        var sprintWorkItem = await _unitOfWork.Repository<SprintWorkItem>()
            .Query()
            .FirstOrDefaultAsync(swi => swi.WorkItemId == request.WorkItemId, cancellationToken);

        if (sprintWorkItem is not null)
            await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(sprintWorkItem, cancellationToken);

        // Calculate new BacklogOrder using midpoint algorithm
        var backlogOrder = await CalculateBacklogOrderAsync(
            workItem.ProjectId, request.AfterWorkItemId, cancellationToken);

        workItem.BacklogOrder = backlogOrder;
        await _unitOfWork.Repository<WorkItem>().UpdateAsync(workItem, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Successfully moved work item {WorkItemId} to backlog", request.WorkItemId);
        return Result<MoveWorkItemToBacklogResponse>.Success(new MoveWorkItemToBacklogResponse
        {
            WorkItemId = request.WorkItemId,
            BacklogOrder = backlogOrder
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while moving work item {WorkItemId} to backlog", request.WorkItemId);
            return Result<MoveWorkItemToBacklogResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }

    private async Task<float> CalculateBacklogOrderAsync(
        Guid projectId, Guid? afterWorkItemId, CancellationToken cancellationToken)
    {
        var sprintWorkItemIds = await _unitOfWork.Repository<SprintWorkItem>()
            .Query()
            .Select(swi => swi.WorkItemId)
            .ToListAsync(cancellationToken);

        var backlogItems = await _unitOfWork.Repository<WorkItem>()
            .Query()
            .Where(w => w.ProjectId == projectId && !sprintWorkItemIds.Contains(w.WorkItemId))
            .OrderBy(w => w.BacklogOrder)
            .Select(w => new { w.WorkItemId, w.BacklogOrder })
            .ToListAsync(cancellationToken);

        if (!backlogItems.Any()) return 1000f;
        if (afterWorkItemId is null) return backlogItems[0].BacklogOrder / 2f;

        var afterIndex = backlogItems.FindIndex(i => i.WorkItemId == afterWorkItemId);

        if (afterIndex < 0) return backlogItems[^1].BacklogOrder + 1000f;
        if (afterIndex == backlogItems.Count - 1) return backlogItems[^1].BacklogOrder + 1000f;

        return (backlogItems[afterIndex].BacklogOrder + backlogItems[afterIndex + 1].BacklogOrder) / 2f;
    }
}

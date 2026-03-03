using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToBacklog;

public class MoveWorkItemToBacklogHandler
    : IRequestHandler<MoveWorkItemToBacklogCommand, Result<MoveWorkItemToBacklogResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public MoveWorkItemToBacklogHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<MoveWorkItemToBacklogResponse>> Handle(
        MoveWorkItemToBacklogCommand request, CancellationToken cancellationToken)
    {
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

        return Result<MoveWorkItemToBacklogResponse>.Success(new MoveWorkItemToBacklogResponse
        {
            WorkItemId = request.WorkItemId,
            BacklogOrder = backlogOrder
        });
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

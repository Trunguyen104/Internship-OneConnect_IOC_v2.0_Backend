using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToSprint;

public class MoveWorkItemToSprintHandler
    : IRequestHandler<MoveWorkItemToSprintCommand, Result<MoveWorkItemToSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public MoveWorkItemToSprintHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<MoveWorkItemToSprintResponse>> Handle(
        MoveWorkItemToSprintCommand request, CancellationToken cancellationToken)
    {
        var workItemExists = await _unitOfWork.Repository<WorkItem>()
            .ExistsAsync(w => w.WorkItemId == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);
        if (!workItemExists)
            return Result<MoveWorkItemToSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.WorkItem.NotFound), ResultErrorType.NotFound);

        var sprintExists = await _unitOfWork.Repository<Sprint>()
            .ExistsAsync(s => s.SprintId == request.TargetSprintId, cancellationToken);
        if (!sprintExists)
            return Result<MoveWorkItemToSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);

        // Remove from current sprint (if any)
        var existing = await _unitOfWork.Repository<SprintWorkItem>()
            .Query()
            .FirstOrDefaultAsync(swi => swi.WorkItemId == request.WorkItemId, cancellationToken);

        if (existing is not null)
            await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(existing, cancellationToken);

        // Calculate new BoardOrder using midpoint algorithm
        var boardOrder = await CalculateBoardOrderAsync(request.TargetSprintId, request.AfterWorkItemId, cancellationToken);

        var sprintWorkItem = new SprintWorkItem
        {
            SprintId = request.TargetSprintId,
            WorkItemId = request.WorkItemId,
            BoardOrder = boardOrder
        };

        await _unitOfWork.Repository<SprintWorkItem>().AddAsync(sprintWorkItem, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<MoveWorkItemToSprintResponse>.Success(new MoveWorkItemToSprintResponse
        {
            WorkItemId = request.WorkItemId,
            SprintId = request.TargetSprintId,
            BoardOrder = boardOrder
        });
    }

    private async Task<float> CalculateBoardOrderAsync(
        Guid sprintId, Guid? afterWorkItemId, CancellationToken cancellationToken)
    {
        var items = await _unitOfWork.Repository<SprintWorkItem>()
            .Query()
            .Where(swi => swi.SprintId == sprintId)
            .OrderBy(swi => swi.BoardOrder)
            .Select(swi => new { swi.WorkItemId, swi.BoardOrder })
            .ToListAsync(cancellationToken);

        if (!items.Any()) return 1000f;
        if (afterWorkItemId is null) return items[0].BoardOrder / 2f;

        var afterIndex = items.FindIndex(i => i.WorkItemId == afterWorkItemId);

        if (afterIndex < 0) return items[^1].BoardOrder + 1000f;
        if (afterIndex == items.Count - 1) return items[^1].BoardOrder + 1000f;

        return (items[afterIndex].BoardOrder + items[afterIndex + 1].BoardOrder) / 2f;
    }
}

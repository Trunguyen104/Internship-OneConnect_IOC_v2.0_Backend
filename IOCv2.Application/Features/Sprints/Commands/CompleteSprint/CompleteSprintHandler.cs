using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public class CompleteSprintHandler : IRequestHandler<CompleteSprintCommand, Result<CompleteSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;

    public CompleteSprintHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _messageService = messageService;
    }

    public async Task<Result<CompleteSprintResponse>> Handle(
        CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _unitOfWork.Repository<Sprint>().Query()
            .FirstOrDefaultAsync(s => s.SprintId == request.SprintId, cancellationToken);

        if (sprint is null)
            return Result<CompleteSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);

        if (sprint.Status != SprintStatus.Active)
            return Result<CompleteSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotActive), ResultErrorType.BadRequest);

        if (!Enum.TryParse<MoveIncompleteItemsOption>(request.IncompleteItemsOption, ignoreCase: true, out var incompleteOption))
            return Result<CompleteSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.InvalidIncompleteItemsOption), ResultErrorType.BadRequest);

        // Load sprint work items
        var sprintWorkItems = await _unitOfWork.Repository<SprintWorkItem>().Query()
            .Where(swi => swi.SprintId == request.SprintId)
            .ToListAsync(cancellationToken);

        var workItemIds = sprintWorkItems.Select(swi => swi.WorkItemId).ToList();

        var completedCount = workItemIds.Any()
            ? await _unitOfWork.Repository<WorkItem>().Query()
                .CountAsync(w => workItemIds.Contains(w.WorkItemId) && w.Status == WorkItemStatus.Done, cancellationToken)
            : 0;

        var incompleteCount = workItemIds.Count - completedCount;

        var incompleteWorkItemIds = workItemIds.Any()
            ? await _unitOfWork.Repository<WorkItem>().Query()
                .Where(w => workItemIds.Contains(w.WorkItemId) && w.Status != WorkItemStatus.Done)
                .Select(w => w.WorkItemId)
                .ToListAsync(cancellationToken)
            : new List<Guid>();

        var incompleteSprintWorkItems = sprintWorkItems
            .Where(swi => incompleteWorkItemIds.Contains(swi.WorkItemId))
            .ToList();

        int movedCount = 0;

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            if (incompleteSprintWorkItems.Any())
            {
                switch (incompleteOption)
                {
                    case MoveIncompleteItemsOption.ToBacklog:
                        foreach (var item in incompleteSprintWorkItems)
                            await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(item, cancellationToken);
                        movedCount = incompleteSprintWorkItems.Count;
                        break;

                    case MoveIncompleteItemsOption.ToNextPlannedSprint:
                        Sprint? nextSprint = null;

                        if (request.TargetSprintId.HasValue)
                        {
                            nextSprint = await _unitOfWork.Repository<Sprint>().Query()
                                .FirstOrDefaultAsync(s => s.SprintId == request.TargetSprintId.Value, cancellationToken);

                            if (nextSprint is null)
                                return Result<CompleteSprintResponse>.Failure(
                                    _messageService.GetMessage(MessageKeys.Sprint.TargetSprintNotFound), ResultErrorType.NotFound);
                        }
                        else
                        {
                            nextSprint = await _unitOfWork.Repository<Sprint>().Query()
                                .Where(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Planned)
                                .OrderBy(s => s.StartDate)
                                .FirstOrDefaultAsync(cancellationToken);
                        }

                        if (nextSprint is not null)
                        {
                            foreach (var item in incompleteSprintWorkItems)
                            {
                                // In EF Core, you CANNOT change a property that is part of a key.
                                // We must delete the old relationship and insert a new one.
                                await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(item, cancellationToken);
                                
                                var newItem = new SprintWorkItem
                                {
                                    SprintId = nextSprint.SprintId,
                                    WorkItemId = item.WorkItemId,
                                    BoardOrder = item.BoardOrder
                                };
                                await _unitOfWork.Repository<SprintWorkItem>().AddAsync(newItem, cancellationToken);
                            }
                        }
                        else
                        {
                            foreach (var item in incompleteSprintWorkItems)
                                await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(item, cancellationToken);
                        }
                        movedCount = incompleteSprintWorkItems.Count;
                        break;

                    case MoveIncompleteItemsOption.CreateNewSprint:
                        var sprintName = !string.IsNullOrWhiteSpace(request.NewSprintName)
                            ? request.NewSprintName
                            : $"{sprint.Name} (Continued)";

                        var newSprint = new Sprint
                        {
                            SprintId = Guid.NewGuid(),
                            ProjectId = sprint.ProjectId,
                            Name = sprintName,
                            Goal = "Incomplete items from previous sprint",
                            StartDate = sprint.EndDate?.AddDays(1) ?? DateOnly.FromDateTime(DateTime.UtcNow),
                            EndDate = sprint.EndDate?.AddDays(15) ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                            Status = SprintStatus.Planned,
                            CreatedAt = DateTime.UtcNow
                        };

                        await _unitOfWork.Repository<Sprint>().AddAsync(newSprint, cancellationToken);
                        foreach (var item in incompleteSprintWorkItems)
                        {
                            // Same PK logic deletion
                            await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(item, cancellationToken);
                            
                            var newItem = new SprintWorkItem
                            {
                                SprintId = newSprint.SprintId,
                                WorkItemId = item.WorkItemId,
                                BoardOrder = item.BoardOrder
                            };
                            await _unitOfWork.Repository<SprintWorkItem>().AddAsync(newItem, cancellationToken);
                        }
                        movedCount = incompleteSprintWorkItems.Count;
                        break;
                }
            }

            sprint.Status = SprintStatus.Completed;
            sprint.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await _cacheService.RemoveAsync(SprintCacheKeys.Sprint(request.SprintId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(
            SprintCacheKeys.SprintListPattern(sprint.ProjectId), cancellationToken);

        return Result<CompleteSprintResponse>.Success(new CompleteSprintResponse
        {
            SprintId = request.SprintId,
            CompletedItemsCount = completedCount,
            IncompleteItemsCount = incompleteCount,
            MovedItemsCount = movedCount,
            Message = $"Sprint completed. {completedCount} items completed, {movedCount} items moved."
        });
    }
}

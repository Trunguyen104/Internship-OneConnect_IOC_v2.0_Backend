using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public class CompleteSprintHandler : IRequestHandler<CompleteSprintCommand, Result<CompleteSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<CompleteSprintHandler> _logger;

    public CompleteSprintHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<CompleteSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<CompleteSprintResponse>> Handle(
        CompleteSprintCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing sprint {SprintId} for project {ProjectId}", request.SprintId, request.ProjectId);

        var sprint = await _unitOfWork.Repository<Sprint>().Query()
            .FirstOrDefaultAsync(s => s.SprintId == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);

        if (sprint is null)
        {
            _logger.LogWarning("Sprint {SprintId} not found", request.SprintId);
            return Result<CompleteSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);
        }

        if (sprint.Status != SprintStatus.Active)
        {
            _logger.LogWarning("Sprint {SprintId} is not in Active status", request.SprintId);
            return Result<CompleteSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotActive), ResultErrorType.BadRequest);
        }

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

        var incompleteSprintWorkItems = workItemIds.Any()
            ? await _unitOfWork.Repository<SprintWorkItem>().Query()
                .Where(swi => swi.SprintId == request.SprintId &&
                              _unitOfWork.Repository<WorkItem>().Query()
                                  .Where(w => workItemIds.Contains(w.WorkItemId) && w.Status != WorkItemStatus.Done)
                                  .Select(w => w.WorkItemId)
                                  .Contains(swi.WorkItemId))
                .ToListAsync(cancellationToken)
            : new List<SprintWorkItem>();

        int movedCount = 0;

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

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
                            {
                                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                                return Result<CompleteSprintResponse>.Failure(
                                    _messageService.GetMessage(MessageKeys.Sprint.TargetSprintNotFound), ResultErrorType.NotFound);
                            }
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
                                item.SprintId = nextSprint.SprintId;
                                await _unitOfWork.Repository<SprintWorkItem>().UpdateAsync(item, cancellationToken);
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

                        var newSprint = new Sprint(sprint.ProjectId, sprintName, "Incomplete items from previous sprint");

                        await _unitOfWork.Repository<Sprint>().AddAsync(newSprint, cancellationToken);
                        foreach (var item in incompleteSprintWorkItems)
                        {
                            item.SprintId = newSprint.SprintId;
                            await _unitOfWork.Repository<SprintWorkItem>().UpdateAsync(item, cancellationToken);
                        }
                        movedCount = incompleteSprintWorkItems.Count;
                        break;
                }
            }

            sprint.Complete();

            await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveAsync(SprintCacheKeys.Sprint(sprint.ProjectId, request.SprintId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                SprintCacheKeys.SprintListPattern(sprint.ProjectId), cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully completed sprint {SprintId}", request.SprintId);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while completing sprint {SprintId}", request.SprintId);
            throw;
        }

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

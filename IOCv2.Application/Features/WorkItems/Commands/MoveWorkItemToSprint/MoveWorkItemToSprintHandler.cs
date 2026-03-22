using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.WorkItems.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToSprint;

public class MoveWorkItemToSprintHandler
    : IRequestHandler<MoveWorkItemToSprintCommand, Result<MoveWorkItemToSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<MoveWorkItemToSprintHandler> _logger;
    private readonly ICacheService _cacheService;

    public MoveWorkItemToSprintHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<MoveWorkItemToSprintHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<MoveWorkItemToSprintResponse>> Handle(
        MoveWorkItemToSprintCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Moving work item {WorkItemId} to sprint {TargetSprintId}", request.WorkItemId, request.TargetSprintId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
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

            // Lấy thông tin record hiện tại (nếu có)
            var existing = await _unitOfWork.Repository<SprintWorkItem>()
                .Query()
                .FirstOrDefaultAsync(swi => swi.WorkItemId == request.WorkItemId, cancellationToken);

            // Calculate new BoardOrder using midpoint algorithm
            var boardOrder = await CalculateBoardOrderAsync(request.TargetSprintId, request.AfterWorkItemId, cancellationToken);

            if (existing is null)
            {
                // Task chưa thuộc sprint nào -> Thêm mới hoàn toàn
                var sprintWorkItem = new SprintWorkItem
                {
                    SprintId = request.TargetSprintId,
                    WorkItemId = request.WorkItemId,
                    BoardOrder = boardOrder
                };
                await _unitOfWork.Repository<SprintWorkItem>().AddAsync(sprintWorkItem, cancellationToken);
            }
            else if (existing.SprintId == request.TargetSprintId)
            {
                // Task di chuyển vị trí TRONG CÙNG 1 SPRINT -> Chỉ sửa thuộc tính BoardOrder (không sửa PK)
                existing.BoardOrder = boardOrder;
                await _unitOfWork.Repository<SprintWorkItem>().UpdateAsync(existing, cancellationToken);
            }
            else
            {
                // Task ĐỔI TỪ SPRINT A SANG SPRINT B -> Xóa cũ + Tạo mới (tránh trùng lặp Primary Key)
                await _unitOfWork.Repository<SprintWorkItem>().DeleteAsync(existing, cancellationToken);

                var newSprintWorkItem = new SprintWorkItem
                {
                    SprintId = request.TargetSprintId,
                    WorkItemId = request.WorkItemId,
                    BoardOrder = boardOrder
                };
              await _unitOfWork.Repository<SprintWorkItem>().AddAsync(newSprintWorkItem, cancellationToken);
            }

        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(WorkItemCacheKeys.BacklogPattern(request.ProjectId), cancellationToken);
        await _cacheService.RemoveAsync(WorkItemCacheKeys.WorkItem(request.WorkItemId), cancellationToken);

        _logger.LogInformation("Successfully moved work item {WorkItemId} to sprint {TargetSprintId}", request.WorkItemId, request.TargetSprintId);
        return Result<MoveWorkItemToSprintResponse>.Success(new MoveWorkItemToSprintResponse
        {
            WorkItemId = request.WorkItemId,
            SprintId = request.TargetSprintId,
            BoardOrder = boardOrder
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while moving work item {WorkItemId} to sprint", request.WorkItemId);
            return Result<MoveWorkItemToSprintResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
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

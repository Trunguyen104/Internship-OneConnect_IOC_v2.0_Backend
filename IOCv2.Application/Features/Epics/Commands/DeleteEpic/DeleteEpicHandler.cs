using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Epics.Commands.DeleteEpic;

public class DeleteEpicHandler : IRequestHandler<DeleteEpicCommand, Result<DeleteEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeleteEpicHandler> _logger;

    public DeleteEpicHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<DeleteEpicHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<DeleteEpicResponse>> Handle(
        DeleteEpicCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting epic: {EpicId} in project: {ProjectId}", request.EpicId, request.ProjectId);

        try
        {
            var epics = await _unitOfWork.Repository<WorkItem>()
                .FindAsync(w => w.WorkItemId == request.EpicId && w.ProjectId == request.ProjectId && w.Type == WorkItemType.Epic, cancellationToken);
            var epic = epics.FirstOrDefault();

            if (epic is null)
            {
                _logger.LogWarning("Epic not found: {EpicId}", request.EpicId);
                return Result<DeleteEpicResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Epic.NotFound), ResultErrorType.NotFound);
            }

            var childrenCount = await _unitOfWork.Repository<WorkItem>()
                .CountAsync(w => w.ParentId == request.EpicId, cancellationToken);

            if (childrenCount > 0)
            {
                _logger.LogWarning("Cannot delete epic {EpicId}: has {Count} children", request.EpicId, childrenCount);
                return Result<DeleteEpicResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Epic.CannotDeleteWithChildren), ResultErrorType.BadRequest);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            await _unitOfWork.Repository<WorkItem>().DeleteAsync(epic, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveAsync(EpicCacheKeys.Epic(epic.ProjectId, request.EpicId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                EpicCacheKeys.EpicListPattern(epic.ProjectId), cancellationToken);

            _logger.LogInformation("Epic soft-deleted successfully: {EpicId}", request.EpicId);
            return Result<DeleteEpicResponse>.Success(new DeleteEpicResponse { Id = request.EpicId });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to delete epic: {EpicId}", request.EpicId);
            return Result<DeleteEpicResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

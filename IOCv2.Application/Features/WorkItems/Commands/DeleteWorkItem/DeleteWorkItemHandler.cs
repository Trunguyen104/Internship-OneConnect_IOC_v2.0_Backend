using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.WorkItems.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.WorkItems.Commands.DeleteWorkItem;

public class DeleteWorkItemHandler : IRequestHandler<DeleteWorkItemCommand, Result<DeleteWorkItemResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeleteWorkItemHandler> _logger;
    private readonly ICacheService _cacheService;

    public DeleteWorkItemHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<DeleteWorkItemHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<DeleteWorkItemResponse>> Handle(
        DeleteWorkItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting work item {WorkItemId} for project {ProjectId}", request.WorkItemId, request.ProjectId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var workItem = await _unitOfWork.Repository<WorkItem>()
            .Query()
            .FirstOrDefaultAsync(w => w.WorkItemId == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);

        if (workItem is null)
            return Result<DeleteWorkItemResponse>.Failure(
                _messageService.GetMessage(MessageKeys.WorkItem.NotFound), ResultErrorType.NotFound);

        await _unitOfWork.Repository<WorkItem>().DeleteAsync(workItem, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(WorkItemCacheKeys.BacklogPattern(request.ProjectId), cancellationToken);
        await _cacheService.RemoveAsync(WorkItemCacheKeys.WorkItem(request.WorkItemId), cancellationToken);

        _logger.LogInformation("Successfully deleted work item {WorkItemId}", request.WorkItemId);
        return Result<DeleteWorkItemResponse>.Success(new DeleteWorkItemResponse
        {
            WorkItemId = workItem.WorkItemId,
            Title = workItem.Title
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while deleting work item {WorkItemId}", request.WorkItemId);
            return Result<DeleteWorkItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.WorkItems.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;

public class UpdateWorkItemHandler : IRequestHandler<UpdateWorkItemCommand, Result<UpdateWorkItemResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly ILogger<UpdateWorkItemHandler> _logger;
    private readonly ICacheService _cacheService;

    public UpdateWorkItemHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ILogger<UpdateWorkItemHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<UpdateWorkItemResponse>> Handle(
        UpdateWorkItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating work item {WorkItemId} for project {ProjectId}", request.WorkItemId, request.ProjectId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var workItem = await _unitOfWork.Repository<WorkItem>()
                .Query()
                .FirstOrDefaultAsync(w => w.WorkItemId == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);

            if (workItem is null)
                return Result<UpdateWorkItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.WorkItem.NotFound), ResultErrorType.NotFound);

            if (request.Title is not null)
                workItem.Title = request.Title;

            if (request.Description is not null)
                workItem.Description = request.Description;

            if (request.StoryPoint.HasValue)
                workItem.StoryPoint = request.StoryPoint;

            if (request.AssigneeId.HasValue)
                workItem.AssigneeId = request.AssigneeId.Value == Guid.Empty ? null : request.AssigneeId.Value;

            if (request.ParentId.HasValue)
                workItem.ParentId = request.ParentId.Value == Guid.Empty ? null : request.ParentId.Value;

            if (request.Type.HasValue)
                workItem.Type = request.Type.Value;

            if (request.DueDate.HasValue)
                workItem.DueDate = request.DueDate;

            if (request.Priority.HasValue)
                workItem.Priority = request.Priority.Value;

            if (request.Status.HasValue)
                workItem.Status = request.Status.Value;

            workItem.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<WorkItem>().UpdateAsync(workItem, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(WorkItemCacheKeys.BacklogPattern(request.ProjectId), cancellationToken);
        await _cacheService.RemoveAsync(WorkItemCacheKeys.WorkItem(request.WorkItemId), cancellationToken);

        _logger.LogInformation("Successfully updated work item {WorkItemId}", request.WorkItemId);
        return Result<UpdateWorkItemResponse>.Success(_mapper.Map<UpdateWorkItemResponse>(workItem));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while updating work item {WorkItemId}", request.WorkItemId);
            return Result<UpdateWorkItemResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

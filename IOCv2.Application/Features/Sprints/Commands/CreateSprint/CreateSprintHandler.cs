using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Features.WorkItems.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public class CreateSprintHandler : IRequestHandler<CreateSprintCommand, Result<CreateSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateSprintHandler> _logger;
    private readonly IMessageService _messageService;

    public CreateSprintHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<CreateSprintHandler> logger,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
        _messageService = messageService;
    }

    public async Task<Result<CreateSprintResponse>> Handle(
        CreateSprintCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating a new sprint for project: {ProjectId}", request.ProjectId);

        var hasWorkItems = request.WorkItemIds is { Count: > 0 };

        // Validate workItems nếu được cung cấp
        if (hasWorkItems)
        {
            var validIds = await _unitOfWork.Repository<WorkItem>().Query()
                .AsNoTracking()
                .Where(w => request.WorkItemIds!.Contains(w.WorkItemId)
                         && w.ProjectId == request.ProjectId
                         && w.Type != WorkItemType.Epic) // Epic không thể add vào Sprint
                .Select(w => w.WorkItemId)
                .ToListAsync(cancellationToken);

            var invalidIds = request.WorkItemIds!.Except(validIds).ToList();
            if (invalidIds.Count > 0)
            {
                _logger.LogWarning("Invalid workItemIds provided for sprint creation: {InvalidIds}", invalidIds);
                return Result<CreateSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.WorkItem.NotFound),
                    ResultErrorType.BadRequest);
            }
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var sprint = new Sprint(request.ProjectId, request.Name, request.Goal);

            await _unitOfWork.Repository<Sprint>().AddAsync(sprint, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Thêm workItems vào sprint nếu có
            if (hasWorkItems)
            {
                var boardOrder = 0;
                foreach (var workItemId in request.WorkItemIds!)
                {
                    var sprintWorkItem = new SprintWorkItem
                    {
                        SprintId = sprint.SprintId,
                        WorkItemId = workItemId,
                        BoardOrder = boardOrder++
                    };
                    await _unitOfWork.Repository<SprintWorkItem>().AddAsync(sprintWorkItem, cancellationToken);
                }
                await _unitOfWork.SaveChangeAsync(cancellationToken);
            }

            await _cacheService.RemoveByPatternAsync(
                SprintCacheKeys.SprintListPattern(request.ProjectId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                WorkItemCacheKeys.BacklogPattern(request.ProjectId), cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully created sprint {SprintId} for project {ProjectId}", sprint.SprintId, request.ProjectId);

            var response = _mapper.Map<CreateSprintResponse>(sprint);
            response.WorkItemCount = hasWorkItems ? request.WorkItemIds!.Count : 0;

            return Result<CreateSprintResponse>.Success(response);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while creating sprint for project: {ProjectId}", request.ProjectId);
            return Result<CreateSprintResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

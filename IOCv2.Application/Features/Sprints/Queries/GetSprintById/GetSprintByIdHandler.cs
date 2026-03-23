using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprintById;

public class GetSprintByIdHandler : IRequestHandler<GetSprintByIdQuery, Result<GetSprintByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetSprintByIdHandler> _logger;

    public GetSprintByIdHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<GetSprintByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetSprintByIdResponse>> Handle(
        GetSprintByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprint {SprintId} for project {ProjectId}", request.SprintId, request.ProjectId);

        var cacheKey = SprintCacheKeys.Sprint(request.ProjectId, request.SprintId);

        var cachedResult = await _cacheService.GetAsync<GetSprintByIdResponse>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            _logger.LogInformation("Returning cached sprint {SprintId}", request.SprintId);
            return Result<GetSprintByIdResponse>.Success(cachedResult);
        }


        var sprint = await _unitOfWork.Repository<Sprint>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SprintId == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);

        if (sprint is null)
        {
            _logger.LogWarning("Sprint {SprintId} not found in project {ProjectId}", request.SprintId, request.ProjectId);
            return Result<GetSprintByIdResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);
        }

        // Load work item stats efficiently — single DB query each
        var workItemIds = await _unitOfWork.Repository<SprintWorkItem>().Query()
            .AsNoTracking()
            .Where(swi => swi.SprintId == request.SprintId)
            .Select(swi => swi.WorkItemId)
            .ToListAsync(cancellationToken);

        var completedCount = workItemIds.Any()
            ? await _unitOfWork.Repository<WorkItem>().Query()
                .AsNoTracking()
                .CountAsync(w => workItemIds.Contains(w.WorkItemId) && w.Status == WorkItemStatus.Done, cancellationToken)
            : 0;

        var response = _mapper.Map<GetSprintByIdResponse>(sprint);
        response.TotalWorkItems = workItemIds.Count;
        response.CompletedWorkItems = completedCount;

        await _cacheService.SetAsync(cacheKey, response, SprintCacheKeys.Expiration.Sprint, cancellationToken);

        _logger.LogInformation("Successfully retrieved sprint {SprintId}", request.SprintId);

        return Result<GetSprintByIdResponse>.Success(response);

    }
}

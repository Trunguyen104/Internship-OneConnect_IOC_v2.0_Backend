using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.WorkItems.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.WorkItems.Queries.GetWorkItemById;

public class GetWorkItemByIdHandler : IRequestHandler<GetWorkItemByIdQuery, Result<GetWorkItemByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetWorkItemByIdHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetWorkItemByIdHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ILogger<GetWorkItemByIdHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<GetWorkItemByIdResponse>> Handle(
        GetWorkItemByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting work item {WorkItemId} for project {ProjectId}", request.WorkItemId, request.ProjectId);

        try
        {
        var cacheKey = WorkItemCacheKeys.WorkItem(request.WorkItemId);
        var cached = await _cacheService.GetAsync<GetWorkItemByIdResponse>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result<GetWorkItemByIdResponse>.Success(cached);

        var workItem = await _unitOfWork.Repository<WorkItem>()
            .Query()
            .AsNoTracking()
            .Include(w => w.Assignee)
                .ThenInclude(s => s!.User)
            .FirstOrDefaultAsync(w => w.WorkItemId == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);

        if (workItem is null)
        {
            return Result<GetWorkItemByIdResponse>.NotFound(
                _messageService.GetMessage(MessageKeys.Error.WorkItemNotFound, request.WorkItemId));
        }

        var response = _mapper.Map<GetWorkItemByIdResponse>(workItem);
        await _cacheService.SetAsync(cacheKey, response, WorkItemCacheKeys.Expiration.WorkItem, cancellationToken);
        return Result<GetWorkItemByIdResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting work item {WorkItemId}", request.WorkItemId);
            return Result<GetWorkItemByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

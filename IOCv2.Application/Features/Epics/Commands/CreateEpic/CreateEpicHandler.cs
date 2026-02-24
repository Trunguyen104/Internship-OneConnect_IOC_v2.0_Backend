using System.Diagnostics;
using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Epics.Commands.CreateEpic;

public class CreateEpicHandler : IRequestHandler<CreateEpicCommand, Result<CreateEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateEpicHandler> _logger;
    
    public CreateEpicHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        ICacheService cacheService,
        ILogger<CreateEpicHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
    }
    
    public async Task<Result<CreateEpicResponse>> Handle(CreateEpicCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Create Epic WorkItem
        var epic = new WorkItem
        {
            WorkItemId = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            ParentId = null, // Epic is always root level
            Type = WorkItemType.Epic,
            Title = request.Name,
            Description = request.Description,
            
            // Epic does not use these fields - set to null/default
            Priority = null,
            Status = null,
            BacklogOrder = 0,
            StoryPoint = null,
            DueDate = null
        };
        
        await _unitOfWork.Repository<WorkItem>().AddAsync(epic, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        
        // Invalidate project epic list cache
        try
        {
            var cachePattern = EpicCacheKeys.EpicListPattern(request.ProjectId);
            await _cacheService.RemoveByPatternAsync(cachePattern, cancellationToken);
            _logger.LogDebug(
                "Invalidated Epic list cache for Project {ProjectId} after creation",
                request.ProjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache after Epic creation");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Epic created: {EpicId} in Project {ProjectId} (Duration: {Duration}ms)",
            epic.WorkItemId, request.ProjectId, stopwatch.ElapsedMilliseconds);
        
        var response = _mapper.Map<CreateEpicResponse>(epic);
        return Result<CreateEpicResponse>.Success(response);
    }
}

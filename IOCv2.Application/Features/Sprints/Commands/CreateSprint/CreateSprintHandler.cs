using System.Diagnostics;
using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Resources;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public class CreateSprintHandler : IRequestHandler<CreateSprintCommand, Result<CreateSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IStringLocalizer<Messages> _messageLocalizer;
    private readonly ILogger<CreateSprintHandler> _logger;
    
    public CreateSprintHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IStringLocalizer<Messages> messageLocalizer,
        ILogger<CreateSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageLocalizer = messageLocalizer;
        _logger = logger;
    }
    
    public async Task<Result<CreateSprintResponse>> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Create sprint entity
        var sprint = new Sprint
        {
            SprintId = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Name,
            Goal = request.Goal,
            StartDate = null,  // Will be set when sprint is started
            EndDate = null,    // Will be set when sprint is started
            Status = SprintStatus.Planned,
            CreatedAt = DateTime.UtcNow
        };
        
        await _unitOfWork.Repository<Sprint>().AddAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // Invalidate cache
        try
        {
            var listCachePattern = SprintCacheKeys.SprintListPattern(request.ProjectId);
            await _cacheService.RemoveByPatternAsync(listCachePattern, cancellationToken);
            
            _logger.LogDebug(
                "Invalidated Sprint list cache for Project {ProjectId}",
                request.ProjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache after Sprint creation");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Sprint created: {SprintId} in Project {ProjectId} (Duration: {Duration}ms)",
            sprint.SprintId, request.ProjectId, stopwatch.ElapsedMilliseconds);
        
        var response = _mapper.Map<CreateSprintResponse>(sprint);
        return Result<CreateSprintResponse>.Success(response);
    }
}

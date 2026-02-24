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

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public class UpdateSprintHandler : IRequestHandler<UpdateSprintCommand, Result<UpdateSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;
    private readonly ILogger<UpdateSprintHandler> _logger;
    
    public UpdateSprintHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        ILogger<UpdateSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _errorLocalizer = errorLocalizer;
        _logger = logger;
    }
    
    public async Task<Result<UpdateSprintResponse>> Handle(UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Find sprint
        var sprints = await _unitOfWork.Repository<Sprint>()
            .FindAsync(s => s.SprintId == request.SprintId, cancellationToken);
        var sprint = sprints.FirstOrDefault();
        
        if (sprint == null)
        {
            stopwatch.Stop();
            _logger.LogWarning("Sprint not found for update: {SprintId}", request.SprintId);
            return Result<UpdateSprintResponse>.NotFound(_errorLocalizer["Sprint.NotFound"]);
        }
        
        // Business rule: Cannot edit Completed sprints
        if (sprint.Status == SprintStatus.Completed)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "Cannot edit completed Sprint {SprintId}",
                request.SprintId);
            return Result<UpdateSprintResponse>.Failure(
                _errorLocalizer["Sprint.CannotEditCompleted"], 
                ResultErrorType.BadRequest);
        }
        
        // Update sprint properties
        sprint.Name = request.Name;
        sprint.Goal = request.Goal;
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;
        sprint.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        
        // Invalidate cache
        try
        {
            var sprintCacheKey = SprintCacheKeys.Sprint(request.SprintId);
            await _cacheService.RemoveAsync(sprintCacheKey, cancellationToken);
            
            var listCachePattern = SprintCacheKeys.SprintListPattern(sprint.ProjectId);
            await _cacheService.RemoveByPatternAsync(listCachePattern, cancellationToken);
            
            _logger.LogDebug(
                "Invalidated Sprint cache for {SprintId} and list for Project {ProjectId}",
                request.SprintId, sprint.ProjectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cache after Sprint update");
        }
        
        stopwatch.Stop();
        _logger.LogInformation(
            "Sprint updated: {SprintId} (Duration: {Duration}ms)",
            request.SprintId, stopwatch.ElapsedMilliseconds);
        
        var response = _mapper.Map<UpdateSprintResponse>(sprint);
        return Result<UpdateSprintResponse>.Success(response);
    }
}

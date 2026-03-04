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

namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

public class StartSprintHandler : IRequestHandler<StartSprintCommand, Result<StartSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<StartSprintHandler> _logger;

    public StartSprintHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<StartSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<StartSprintResponse>> Handle(
        StartSprintCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting sprint {SprintId} for project {ProjectId}", request.SprintId, request.ProjectId);

        try
        {
            var sprint = await _unitOfWork.Repository<Sprint>().Query()
                .FirstOrDefaultAsync(s => s.SprintId == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);

            if (sprint is null)
            {
                _logger.LogWarning("Sprint {SprintId} not found", request.SprintId);
                return Result<StartSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);
            }

            if (sprint.Status != SprintStatus.Planned)
            {
                _logger.LogWarning("Sprint {SprintId} is not in Planned status", request.SprintId);
                return Result<StartSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.NotPlanned), ResultErrorType.BadRequest);
            }

            var hasActiveSprint = await _unitOfWork.Repository<Sprint>().Query()
                .AnyAsync(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active, cancellationToken);

            if (hasActiveSprint)
            {
                _logger.LogWarning("An active sprint already exists for project {ProjectId}", request.ProjectId);
                return Result<StartSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.ActiveSprintExists), ResultErrorType.BadRequest);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            sprint.Start(request.StartDate, request.EndDate);

            await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveAsync(SprintCacheKeys.Sprint(sprint.ProjectId, request.SprintId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                SprintCacheKeys.SprintListPattern(sprint.ProjectId), cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully started sprint {SprintId}", request.SprintId);

            return Result<StartSprintResponse>.Success(_mapper.Map<StartSprintResponse>(sprint));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while starting sprint {SprintId}", request.SprintId);
            throw;
        }
    }
}

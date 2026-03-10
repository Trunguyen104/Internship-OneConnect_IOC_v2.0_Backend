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

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public class UpdateSprintHandler : IRequestHandler<UpdateSprintCommand, Result<UpdateSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<UpdateSprintHandler> _logger;

    public UpdateSprintHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<UpdateSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<UpdateSprintResponse>> Handle(
        UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating sprint {SprintId} for project {ProjectId}", request.SprintId, request.ProjectId);

        try
        {
            var sprint = await _unitOfWork.Repository<Sprint>().Query()
                .FirstOrDefaultAsync(s => s.SprintId == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);

            if (sprint is null)
            {
                _logger.LogWarning("Sprint {SprintId} not found for project {ProjectId}", request.SprintId, request.ProjectId);
                return Result<UpdateSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);
            }

            if (sprint.Status == SprintStatus.Completed)
            {
                _logger.LogWarning("Attempted to update completed sprint {SprintId}", request.SprintId);
                return Result<UpdateSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.CannotEditCompleted), ResultErrorType.BadRequest);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            sprint.Update(request.Name, request.Goal, request.StartDate, request.EndDate);

            await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveAsync(SprintCacheKeys.Sprint(sprint.ProjectId, request.SprintId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                SprintCacheKeys.SprintListPattern(sprint.ProjectId), cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully updated sprint {SprintId}", request.SprintId);

            return Result<UpdateSprintResponse>.Success(_mapper.Map<UpdateSprintResponse>(sprint));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while updating sprint {SprintId}", request.SprintId);
            return Result<UpdateSprintResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

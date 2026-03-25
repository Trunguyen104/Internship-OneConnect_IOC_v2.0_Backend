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

namespace IOCv2.Application.Features.Sprints.Commands.DeleteSprint;

public class DeleteSprintHandler : IRequestHandler<DeleteSprintCommand, Result<DeleteSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<DeleteSprintHandler> _logger;

    public DeleteSprintHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<DeleteSprintHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<DeleteSprintResponse>> Handle(
        DeleteSprintCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting sprint {SprintId} for project {ProjectId}", request.SprintId, request.ProjectId);

        try
        {
            var sprint = await _unitOfWork.Repository<Sprint>().Query()
                .FirstOrDefaultAsync(s => s.SprintId == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);

            if (sprint is null)
            {
                _logger.LogWarning("Sprint {SprintId} not found", request.SprintId);
                return Result<DeleteSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);
            }

            if (sprint.Status != SprintStatus.Planned)
            {
                _logger.LogWarning("Cannot delete sprint {SprintId} as it is not in Planned status", request.SprintId);
                return Result<DeleteSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.CannotDeleteActiveSprint), ResultErrorType.BadRequest);
            }

            var hasWorkItems = await _unitOfWork.Repository<SprintWorkItem>().Query()
                .AnyAsync(swi => swi.SprintId == request.SprintId, cancellationToken);

            if (hasWorkItems)
            {
                _logger.LogWarning("Cannot delete sprint {SprintId} as it has work items", request.SprintId);
                return Result<DeleteSprintResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.CannotDeleteWithWorkItems), ResultErrorType.BadRequest);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            await _unitOfWork.Repository<Sprint>().DeleteAsync(sprint, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveAsync(SprintCacheKeys.Sprint(sprint.ProjectId, request.SprintId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                SprintCacheKeys.SprintListPattern(sprint.ProjectId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                WorkItemCacheKeys.BacklogPattern(sprint.ProjectId), cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted sprint {SprintId}", request.SprintId);

            return Result<DeleteSprintResponse>.Success(_mapper.Map<DeleteSprintResponse>(sprint));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while deleting sprint {SprintId}", request.SprintId);
            return Result<DeleteSprintResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

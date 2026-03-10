using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

public class UpdateEpicHandler : IRequestHandler<UpdateEpicCommand, Result<UpdateEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<UpdateEpicHandler> _logger;

    public UpdateEpicHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<UpdateEpicHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<UpdateEpicResponse>> Handle(
        UpdateEpicCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating epic: {EpicId} in project: {ProjectId}", request.EpicId, request.ProjectId);

        try
        {
            var epics = await _unitOfWork.Repository<WorkItem>()
                .FindAsync(w => w.WorkItemId == request.EpicId && w.ProjectId == request.ProjectId && w.Type == WorkItemType.Epic, cancellationToken);
            var epic = epics.FirstOrDefault();

            if (epic is null)
            {
                _logger.LogWarning("Epic not found: {EpicId}", request.EpicId);
                return Result<UpdateEpicResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Epic.NotFound), ResultErrorType.NotFound);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            epic.UpdateInfo(request.Name, request.Description);

            await _unitOfWork.Repository<WorkItem>().UpdateAsync(epic, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveAsync(EpicCacheKeys.Epic(epic.ProjectId, request.EpicId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(
                EpicCacheKeys.EpicListPattern(epic.ProjectId), cancellationToken);

            _logger.LogInformation("Epic updated successfully: {EpicId}", request.EpicId);
            return Result<UpdateEpicResponse>.Success(_mapper.Map<UpdateEpicResponse>(epic));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to update epic: {EpicId}", request.EpicId);
            return Result<UpdateEpicResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

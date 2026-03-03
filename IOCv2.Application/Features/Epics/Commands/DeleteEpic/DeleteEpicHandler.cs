using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Epics.Commands.DeleteEpic;

public class DeleteEpicHandler : IRequestHandler<DeleteEpicCommand, Result<DeleteEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;

    public DeleteEpicHandler(
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _messageService = messageService;
    }

    public async Task<Result<DeleteEpicResponse>> Handle(
        DeleteEpicCommand request, CancellationToken cancellationToken)
    {
        var epics = await _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.WorkItemId == request.EpicId && w.ProjectId == request.ProjectId && w.Type == WorkItemType.Epic, cancellationToken);
        var epic = epics.FirstOrDefault();

        if (epic is null)
            return Result<DeleteEpicResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Epic.NotFound), ResultErrorType.NotFound);

        var childrenCount = await _unitOfWork.Repository<WorkItem>()
            .CountAsync(w => w.ParentId == request.EpicId, cancellationToken);

        if (childrenCount > 0)
            return Result<DeleteEpicResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Epic.CannotDeleteWithChildren), ResultErrorType.BadRequest);

        epic.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<WorkItem>().UpdateAsync(epic, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        await _cacheService.RemoveAsync(EpicCacheKeys.Epic(epic.ProjectId, request.EpicId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(
            EpicCacheKeys.EpicListPattern(epic.ProjectId), cancellationToken);

        return Result<DeleteEpicResponse>.Success(new DeleteEpicResponse { Id = request.EpicId });
    }
}

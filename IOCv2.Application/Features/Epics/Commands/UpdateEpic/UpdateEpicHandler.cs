using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

public class UpdateEpicHandler : IRequestHandler<UpdateEpicCommand, Result<UpdateEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;

    public UpdateEpicHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageService = messageService;
    }

    public async Task<Result<UpdateEpicResponse>> Handle(
        UpdateEpicCommand request, CancellationToken cancellationToken)
    {
        var epics = await _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.WorkItemId == request.EpicId && w.Type == WorkItemType.Epic, cancellationToken);
        var epic = epics.FirstOrDefault();

        if (epic is null)
            return Result<UpdateEpicResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Epic.NotFound), ResultErrorType.NotFound);

        epic.Title = request.Name;
        epic.Description = request.Description;
        epic.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<WorkItem>().UpdateAsync(epic, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        await _cacheService.RemoveAsync(EpicCacheKeys.Epic(request.EpicId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(
            EpicCacheKeys.EpicListPattern(epic.ProjectId), cancellationToken);

        return Result<UpdateEpicResponse>.Success(_mapper.Map<UpdateEpicResponse>(epic));
    }
}

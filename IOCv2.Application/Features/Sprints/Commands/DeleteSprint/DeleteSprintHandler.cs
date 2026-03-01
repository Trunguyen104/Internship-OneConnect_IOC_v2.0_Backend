using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Sprints.Commands.DeleteSprint;

public class DeleteSprintHandler : IRequestHandler<DeleteSprintCommand, Result<DeleteSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;

    public DeleteSprintHandler(
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

    public async Task<Result<DeleteSprintResponse>> Handle(
        DeleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _unitOfWork.Repository<Sprint>().Query()
            .FirstOrDefaultAsync(s => s.SprintId == request.SprintId, cancellationToken);

        if (sprint is null)
            return Result<DeleteSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);

        if (sprint.Status != SprintStatus.Planned)
            return Result<DeleteSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.CannotDeleteActiveSprint), ResultErrorType.BadRequest);

        var hasWorkItems = await _unitOfWork.Repository<SprintWorkItem>().Query()
            .AnyAsync(swi => swi.SprintId == request.SprintId, cancellationToken);

        if (hasWorkItems)
            return Result<DeleteSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.CannotDeleteWithWorkItems), ResultErrorType.BadRequest);

        await _unitOfWork.Repository<Sprint>().DeleteAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        await _cacheService.RemoveAsync(SprintCacheKeys.Sprint(request.SprintId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(
            SprintCacheKeys.SprintListPattern(sprint.ProjectId), cancellationToken);

        return Result<DeleteSprintResponse>.Success(_mapper.Map<DeleteSprintResponse>(sprint));
    }
}

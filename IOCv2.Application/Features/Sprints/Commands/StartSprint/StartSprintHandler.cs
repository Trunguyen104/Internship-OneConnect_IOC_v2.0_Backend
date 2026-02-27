using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

public class StartSprintHandler : IRequestHandler<StartSprintCommand, Result<StartSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;

    public StartSprintHandler(
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

    public async Task<Result<StartSprintResponse>> Handle(
        StartSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _unitOfWork.Repository<Sprint>().Query()
            .FirstOrDefaultAsync(s => s.SprintId == request.SprintId, cancellationToken);

        if (sprint is null)
            return Result<StartSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);

        if (sprint.Status != SprintStatus.Planned)
            return Result<StartSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotPlanned), ResultErrorType.BadRequest);

        var hasActiveSprint = await _unitOfWork.Repository<Sprint>().Query()
            .AnyAsync(s => s.ProjectId == sprint.ProjectId && s.Status == SprintStatus.Active, cancellationToken);

        if (hasActiveSprint)
            return Result<StartSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.ActiveSprintExists), ResultErrorType.BadRequest);

        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;
        sprint.Status = SprintStatus.Active;
        sprint.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        await _cacheService.RemoveAsync(SprintCacheKeys.Sprint(request.SprintId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(
            SprintCacheKeys.SprintListPattern(sprint.ProjectId), cancellationToken);

        return Result<StartSprintResponse>.Success(_mapper.Map<StartSprintResponse>(sprint));
    }
}

using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Sprints.Commands.UpdateSprint;

public class UpdateSprintHandler : IRequestHandler<UpdateSprintCommand, Result<UpdateSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;

    public UpdateSprintHandler(
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

    public async Task<Result<UpdateSprintResponse>> Handle(
        UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _unitOfWork.Repository<Sprint>().Query()
            .FirstOrDefaultAsync(s => s.SprintId == request.SprintId && s.ProjectId == request.ProjectId, cancellationToken);

        if (sprint is null)
            return Result<UpdateSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);

        if (sprint.Status == SprintStatus.Completed)
            return Result<UpdateSprintResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Sprint.CannotEditCompleted), ResultErrorType.BadRequest);

        sprint.Name = request.Name;
        sprint.Goal = request.Goal;
        sprint.StartDate = request.StartDate;
        sprint.EndDate = request.EndDate;
        sprint.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Sprint>().UpdateAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        await _cacheService.RemoveAsync(SprintCacheKeys.Sprint(sprint.ProjectId, request.SprintId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(
            SprintCacheKeys.SprintListPattern(sprint.ProjectId), cancellationToken);

        return Result<UpdateSprintResponse>.Success(_mapper.Map<UpdateSprintResponse>(sprint));
    }
}

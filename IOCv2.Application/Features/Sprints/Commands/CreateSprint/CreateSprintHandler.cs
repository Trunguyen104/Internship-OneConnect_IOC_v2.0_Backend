using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public class CreateSprintHandler : IRequestHandler<CreateSprintCommand, Result<CreateSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public CreateSprintHandler(IUnitOfWork unitOfWork, IMapper mapper, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<Result<CreateSprintResponse>> Handle(
        CreateSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = new Sprint
        {
            SprintId = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Name,
            Goal = request.Goal,
            StartDate = null,
            EndDate = null,
            Status = SprintStatus.Planned,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Sprint>().AddAsync(sprint, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(
            SprintCacheKeys.SprintListPattern(request.ProjectId), cancellationToken);

        return Result<CreateSprintResponse>.Success(_mapper.Map<CreateSprintResponse>(sprint));
    }
}

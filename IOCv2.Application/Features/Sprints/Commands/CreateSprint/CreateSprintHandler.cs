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

namespace IOCv2.Application.Features.Sprints.Commands.CreateSprint;

public class CreateSprintHandler : IRequestHandler<CreateSprintCommand, Result<CreateSprintResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateSprintHandler> _logger;
    private readonly IMessageService _messageService;

    public CreateSprintHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<CreateSprintHandler> logger,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
        _messageService = messageService;
    }

    public async Task<Result<CreateSprintResponse>> Handle(
        CreateSprintCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating a new sprint for project: {ProjectId}", request.ProjectId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var sprint = new Sprint(request.ProjectId, request.Name, request.Goal);

            await _unitOfWork.Repository<Sprint>().AddAsync(sprint, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(
                SprintCacheKeys.SprintListPattern(request.ProjectId), cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully created sprint {SprintId} for project {ProjectId}", sprint.SprintId, request.ProjectId);

            return Result<CreateSprintResponse>.Success(_mapper.Map<CreateSprintResponse>(sprint));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while creating sprint for project: {ProjectId}", request.ProjectId);
            throw;
        }
    }
}

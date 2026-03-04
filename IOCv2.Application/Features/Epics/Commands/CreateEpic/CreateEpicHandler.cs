using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Epics.Commands.CreateEpic;

public class CreateEpicHandler : IRequestHandler<CreateEpicCommand, Result<CreateEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreateEpicHandler> _logger;

    public CreateEpicHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        ILogger<CreateEpicHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<CreateEpicResponse>> Handle(
        CreateEpicCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating epic for project: {ProjectId}", request.ProjectId);

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            var epic = new WorkItem
            {
                WorkItemId = Guid.NewGuid(),
                ProjectId = request.ProjectId,
                ParentId = null,
                Type = WorkItemType.Epic,
                Title = request.Name,
                Description = request.Description,
                Priority = null,
                Status = null,
                BacklogOrder = 0,
                StoryPoint = null,
                DueDate = null
            };

            await _unitOfWork.Repository<WorkItem>().AddAsync(epic, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(
                EpicCacheKeys.EpicListPattern(request.ProjectId), cancellationToken);

            _logger.LogInformation("Epic created successfully: {EpicId}", epic.WorkItemId);
            return Result<CreateEpicResponse>.Success(_mapper.Map<CreateEpicResponse>(epic));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Failed to create epic for project: {ProjectId}", request.ProjectId);
            throw;
        }
    }
}

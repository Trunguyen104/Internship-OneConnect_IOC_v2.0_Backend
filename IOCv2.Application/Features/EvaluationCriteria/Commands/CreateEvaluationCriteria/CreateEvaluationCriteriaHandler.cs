using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.CreateEvaluationCriteria;

public class CreateEvaluationCriteriaHandler
    : IRequestHandler<CreateEvaluationCriteriaCommand, Result<CreateEvaluationCriteriaResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<CreateEvaluationCriteriaHandler> _logger;

    public CreateEvaluationCriteriaHandler(
        IUnitOfWork unitOfWork, 
        IMessageService messageService,
        ILogger<CreateEvaluationCriteriaHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<CreateEvaluationCriteriaResponse>> Handle(
        CreateEvaluationCriteriaCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating EvaluationCriteria {Name} for Cycle {CycleId}", request.Name, request.CycleId);

        var cycleExists = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AnyAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (!cycleExists)
        {
            _logger.LogWarning("EvaluationCycle {CycleId} not found", request.CycleId);
            return Result<CreateEvaluationCriteriaResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.CycleNotFound),
                ResultErrorType.NotFound);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var criteria = new Domain.Entities.EvaluationCriteria
        {
            CriteriaId = Guid.NewGuid(),
            CycleId = request.CycleId,
            Name = request.Name,
            Description = request.Description,
            MaxScore = request.MaxScore,
            Weight = request.Weight
        };

        await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().AddAsync(criteria, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);
        await _unitOfWork.CommitTransactionAsync(cancellationToken);

        _logger.LogInformation("Successfully created EvaluationCriteria {CriteriaId}", criteria.CriteriaId);

        return Result<CreateEvaluationCriteriaResponse>.Success(new CreateEvaluationCriteriaResponse
        {
            CriteriaId = criteria.CriteriaId,
            CycleId = criteria.CycleId,
            Name = criteria.Name,
            Description = criteria.Description,
            MaxScore = criteria.MaxScore,
            Weight = criteria.Weight,
            CreatedAt = criteria.CreatedAt
        });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error occurred while creating EvaluationCriteria for Cycle {CycleId}", request.CycleId);
            return Result<CreateEvaluationCriteriaResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}

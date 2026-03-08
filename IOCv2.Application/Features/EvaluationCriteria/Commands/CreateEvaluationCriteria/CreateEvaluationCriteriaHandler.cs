using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.CreateEvaluationCriteria;

public class CreateEvaluationCriteriaHandler
    : IRequestHandler<CreateEvaluationCriteriaCommand, Result<CreateEvaluationCriteriaResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public CreateEvaluationCriteriaHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<CreateEvaluationCriteriaResponse>> Handle(
        CreateEvaluationCriteriaCommand request, CancellationToken cancellationToken)
    {
        var cycleExists = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AnyAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (!cycleExists)
            return Result<CreateEvaluationCriteriaResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.CycleNotFound),
                ResultErrorType.NotFound);

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
}

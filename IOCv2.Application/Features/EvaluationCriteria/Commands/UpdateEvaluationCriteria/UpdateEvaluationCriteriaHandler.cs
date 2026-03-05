using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.UpdateEvaluationCriteria;

public class UpdateEvaluationCriteriaHandler
    : IRequestHandler<UpdateEvaluationCriteriaCommand, Result<UpdateEvaluationCriteriaResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public UpdateEvaluationCriteriaHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<UpdateEvaluationCriteriaResponse>> Handle(
        UpdateEvaluationCriteriaCommand request, CancellationToken cancellationToken)
    {
        var criteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .FirstOrDefaultAsync(c => c.CriteriaId == request.CriteriaId, cancellationToken);

        if (criteria is null)
            return Result<UpdateEvaluationCriteriaResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.NotFound),
                ResultErrorType.NotFound);

        criteria.Name = request.Name;
        criteria.Description = request.Description;
        criteria.MaxScore = request.MaxScore;
        criteria.Weight = request.Weight;
        criteria.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().UpdateAsync(criteria, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<UpdateEvaluationCriteriaResponse>.Success(new UpdateEvaluationCriteriaResponse
        {
            CriteriaId = criteria.CriteriaId,
            CycleId = criteria.CycleId,
            Name = criteria.Name,
            Description = criteria.Description,
            MaxScore = criteria.MaxScore,
            Weight = criteria.Weight,
            UpdatedAt = criteria.UpdatedAt
        });
    }
}

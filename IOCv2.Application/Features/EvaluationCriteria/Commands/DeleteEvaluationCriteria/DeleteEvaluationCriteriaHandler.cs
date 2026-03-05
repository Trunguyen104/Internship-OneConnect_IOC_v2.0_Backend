using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.DeleteEvaluationCriteria;

public class DeleteEvaluationCriteriaHandler
    : IRequestHandler<DeleteEvaluationCriteriaCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public DeleteEvaluationCriteriaHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<bool>> Handle(
        DeleteEvaluationCriteriaCommand request, CancellationToken cancellationToken)
    {
        var criteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .FirstOrDefaultAsync(c => c.CriteriaId == request.CriteriaId, cancellationToken);

        if (criteria is null)
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCriteriaKey.NotFound),
                ResultErrorType.NotFound);

        criteria.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().UpdateAsync(criteria, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

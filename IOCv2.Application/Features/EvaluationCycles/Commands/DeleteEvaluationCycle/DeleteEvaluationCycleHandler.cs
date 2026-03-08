using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.DeleteEvaluationCycle;

public class DeleteEvaluationCycleHandler
    : IRequestHandler<DeleteEvaluationCycleCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public DeleteEvaluationCycleHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<bool>> Handle(
        DeleteEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.NotFound),
                ResultErrorType.NotFound);

        var hasCriteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
            .AnyAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (hasCriteria)
            return Result<bool>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.CannotDeleteWithCriteria),
                ResultErrorType.BadRequest);

        // Soft delete via UpdatedAt — EF global filter handles DeletedAt
        cycle.DeletedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<EvaluationCycle>().UpdateAsync(cycle, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}

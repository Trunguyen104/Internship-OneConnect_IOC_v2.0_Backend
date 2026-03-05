using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;

public class UpdateEvaluationCycleHandler
    : IRequestHandler<UpdateEvaluationCycleCommand, Result<UpdateEvaluationCycleResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public UpdateEvaluationCycleHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<UpdateEvaluationCycleResponse>> Handle(
        UpdateEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
            return Result<UpdateEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.NotFound),
                ResultErrorType.NotFound);

        if (!Enum.TryParse<EvaluationCycleStatus>(request.Status, ignoreCase: true, out var status))
            return Result<UpdateEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                ResultErrorType.BadRequest);

        cycle.Name = request.Name;
        cycle.StartDate = request.StartDate;
        cycle.EndDate = request.EndDate;
        cycle.Status = status;
        cycle.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<EvaluationCycle>().UpdateAsync(cycle, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<UpdateEvaluationCycleResponse>.Success(new UpdateEvaluationCycleResponse
        {
            CycleId = cycle.CycleId,
            TermId = cycle.TermId,
            Name = cycle.Name,
            StartDate = cycle.StartDate,
            EndDate = cycle.EndDate,
            Status = cycle.Status.ToString(),
            UpdatedAt = cycle.UpdatedAt
        });
    }
}

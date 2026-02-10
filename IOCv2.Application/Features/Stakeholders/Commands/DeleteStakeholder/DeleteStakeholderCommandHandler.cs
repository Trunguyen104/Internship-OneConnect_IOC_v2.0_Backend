using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder;

public class DeleteStakeholderCommandHandler 
    : IRequestHandler<DeleteStakeholderCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<Resources.ErrorMessages> _localizer;

    public DeleteStakeholderCommandHandler(
        IUnitOfWork unitOfWork,
        IStringLocalizer<Resources.ErrorMessages> localizer)
    {
        _unitOfWork = unitOfWork;
        _localizer = localizer;
    }

    public async Task<Result> Handle(
        DeleteStakeholderCommand request, 
        CancellationToken cancellationToken)
    {
        // Get stakeholder
        var stakeholder = await _unitOfWork.Repository<Stakeholder>()
            .Query()
            .Where(s => s.Id == request.Id && s.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (stakeholder == null)
        {
            return Result.NotFound(_localizer["Stakeholder.NotFound"]);
        }

        // Soft delete
        stakeholder.DeletedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Stakeholder>().UpdateAsync(stakeholder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


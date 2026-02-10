using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder;

public class UpdateStakeholderCommandHandler 
    : IRequestHandler<UpdateStakeholderCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<Resources.ErrorMessages> _localizer;

    public UpdateStakeholderCommandHandler(
        IUnitOfWork unitOfWork,
        IStringLocalizer<Resources.ErrorMessages> localizer)
    {
        _unitOfWork = unitOfWork;
        _localizer = localizer;
    }

    public async Task<Result> Handle(
        UpdateStakeholderCommand request, 
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

        // Check email uniqueness if email is being updated
        if (!string.IsNullOrEmpty(request.Email) && 
            request.Email.ToLower() != stakeholder.Email.ToLower())
        {
            var emailExists = await _unitOfWork.Repository<Stakeholder>()
                .ExistsAsync(
                    s => s.ProjectId == stakeholder.ProjectId 
                        && s.Email.ToLower() == request.Email.ToLower() 
                        && s.Id != request.Id 
                        && s.DeletedAt == null, 
                    cancellationToken);

            if (emailExists)
            {
                return Result.Conflict(_localizer["Stakeholder.EmailExists"]);
            }
        }

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Name))
            stakeholder.Name = request.Name;

        if (request.Type.HasValue)
            stakeholder.Type = request.Type.Value;

        if (request.Role != null) // Allow setting to empty string
            stakeholder.Role = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role;

        if (request.Description != null) // Allow setting to empty string
            stakeholder.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;

        if (!string.IsNullOrEmpty(request.Email))
            stakeholder.Email = request.Email;

        if (request.PhoneNumber != null) // Allow setting to empty string
            stakeholder.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber;

        stakeholder.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Stakeholder>().UpdateAsync(stakeholder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}


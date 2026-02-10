using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder;

public class CreateStakeholderCommandHandler 
    : IRequestHandler<CreateStakeholderCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<Resources.ErrorMessages> _localizer;

    public CreateStakeholderCommandHandler(
        IUnitOfWork unitOfWork,
        IStringLocalizer<Resources.ErrorMessages> localizer)
    {
        _unitOfWork = unitOfWork;
        _localizer = localizer;
    }

    public async Task<Result<Guid>> Handle(
        CreateStakeholderCommand request, 
        CancellationToken cancellationToken)
    {
        // Check if project exists
        var projectExists = await _unitOfWork.Repository<Project>()
            .ExistsAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken);

        if (!projectExists)
        {
            return Result<Guid>.NotFound(_localizer["Stakeholder.ProjectNotFound"]);
        }

        // Check if email already exists in this project
        var emailExists = await _unitOfWork.Repository<Stakeholder>()
            .ExistsAsync(
                s => s.ProjectId == request.ProjectId 
                    && s.Email.ToLower() == request.Email.ToLower() 
                    && s.DeletedAt == null, 
                cancellationToken);

        if (emailExists)
        {
            return Result<Guid>.Conflict(_localizer["Stakeholder.EmailExists"]);
        }

        // Create stakeholder
        var stakeholder = new Stakeholder
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            Name = request.Name,
            Type = request.Type,
            Role = request.Role,
            Description = request.Description,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Stakeholder>().AddAsync(stakeholder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(stakeholder.Id);
    }
}


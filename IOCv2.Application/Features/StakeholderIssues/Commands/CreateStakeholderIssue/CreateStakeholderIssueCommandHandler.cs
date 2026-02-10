using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Localization;
using IOCv2.Application.Resources;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue;

public class CreateStakeholderIssueCommandHandler : IRequestHandler<CreateStakeholderIssueCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;
    private readonly IStringLocalizer<Messages> _messageLocalizer;

    public CreateStakeholderIssueCommandHandler(
        IUnitOfWork unitOfWork,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        IStringLocalizer<Messages> messageLocalizer)
    {
        _unitOfWork = unitOfWork;
        _errorLocalizer = errorLocalizer;
        _messageLocalizer = messageLocalizer;
    }

    public async Task<Result<Guid>> Handle(CreateStakeholderIssueCommand request, CancellationToken cancellationToken)
    {
        var stakeholder = await _unitOfWork.Repository<Stakeholder>().GetByIdAsync(request.StakeholderId);
        if (stakeholder == null)
        {
            return Result<Guid>.Failure(_errorLocalizer["Issue.StakeholderNotFound"], ResultErrorType.NotFound);
        }

        var issue = new StakeholderIssue
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            StakeholderId = request.StakeholderId,
            Status = Domain.Enums.StakeholderIssueStatus.Open
        };

        await _unitOfWork.Repository<StakeholderIssue>().AddAsync(issue);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (result > 0)
        {
            return Result<Guid>.Success(issue.Id);
        }

        return Result<Guid>.Failure(_errorLocalizer["Issue.CreateFailed"]);
    }
}

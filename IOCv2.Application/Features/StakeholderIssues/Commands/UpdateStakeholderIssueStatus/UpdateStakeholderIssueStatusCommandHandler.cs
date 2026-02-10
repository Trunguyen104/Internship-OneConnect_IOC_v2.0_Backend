﻿using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Localization;
using IOCv2.Application.Resources;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus;

public class UpdateStakeholderIssueStatusCommandHandler : IRequestHandler<UpdateStakeholderIssueStatusCommand, Result<string>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;
    private readonly IStringLocalizer<Messages> _messageLocalizer;

    public UpdateStakeholderIssueStatusCommandHandler(
        IUnitOfWork unitOfWork,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        IStringLocalizer<Messages> messageLocalizer)
    {
        _unitOfWork = unitOfWork;
        _errorLocalizer = errorLocalizer;
        _messageLocalizer = messageLocalizer;
    }

    public async Task<Result<string>> Handle(UpdateStakeholderIssueStatusCommand request, CancellationToken cancellationToken)
    {
        var issue = await _unitOfWork.Repository<StakeholderIssue>().GetByIdAsync(request.Id);
        if (issue == null)
        {
            return Result<string>.Failure(_errorLocalizer["Issue.NotFound"], ResultErrorType.NotFound);
        }

        issue.Status = request.Status;
        issue.UpdatedAt = DateTime.UtcNow;
        
        if (request.Status == StakeholderIssueStatus.Resolved || request.Status == StakeholderIssueStatus.Closed)
        {
            issue.ResolvedAt = DateTime.UtcNow;
        }
        else
        {
            issue.ResolvedAt = null;
        }

        await _unitOfWork.Repository<StakeholderIssue>().UpdateAsync(issue, cancellationToken);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (result > 0)
        {
            return Result<string>.Success(_messageLocalizer["Issue.UpdateStatusSuccess"]);
        }

        return Result<string>.Failure(_errorLocalizer["Issue.UpdateFailed"]);
    }
}

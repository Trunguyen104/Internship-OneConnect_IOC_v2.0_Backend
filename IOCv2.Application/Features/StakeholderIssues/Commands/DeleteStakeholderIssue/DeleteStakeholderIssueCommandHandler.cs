using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Localization;
using IOCv2.Application.Resources;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue;

public class DeleteStakeholderIssueCommandHandler : IRequestHandler<DeleteStakeholderIssueCommand, Result<string>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<ErrorMessages> _errorLocalizer;
    private readonly IStringLocalizer<Messages> _messageLocalizer;

    public DeleteStakeholderIssueCommandHandler(
        IUnitOfWork unitOfWork,
        IStringLocalizer<ErrorMessages> errorLocalizer,
        IStringLocalizer<Messages> messageLocalizer)
    {
        _unitOfWork = unitOfWork;
        _errorLocalizer = errorLocalizer;
        _messageLocalizer = messageLocalizer;
    }

    public async Task<Result<string>> Handle(DeleteStakeholderIssueCommand request, CancellationToken cancellationToken)
    {
        var issue = await _unitOfWork.Repository<StakeholderIssue>().GetByIdAsync(request.Id);
        if (issue == null)
        {
            return Result<string>.Failure(_errorLocalizer["Issue.NotFound"], ResultErrorType.NotFound);
        }

        await _unitOfWork.Repository<StakeholderIssue>().DeleteAsync(issue, cancellationToken);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (result > 0)
        {
            return Result<string>.Success(_messageLocalizer["Issue.DeleteSuccess"]);
        }

        return Result<string>.Failure(_errorLocalizer["Issue.DeleteFailed"]);
    }
}

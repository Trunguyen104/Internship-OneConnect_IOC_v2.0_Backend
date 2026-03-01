using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus
{
    public record UpdateStakeholderIssueStatusCommand : IRequest<Result<UpdateStakeholderIssueStatusResponse>>
    {
        public Guid Id { get; init; }
        public string Status { get; init; } = string.Empty;
    }
}


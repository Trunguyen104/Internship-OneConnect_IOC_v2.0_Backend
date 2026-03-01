using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue
{
    public record CreateStakeholderIssueCommand : IRequest<Result<CreateStakeholderIssueResponse>>
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public Guid StakeholderId { get; init; }
    }
}


using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue
{
    /// <summary>
    /// Command to create a new stakeholder issue.
    /// </summary>
    public record CreateStakeholderIssueCommand : IRequest<IOCv2.Application.Common.Models.Result<CreateStakeholderIssueResponse>>
    {
        /// <summary>
        /// The title of the stakeholder issue.
        /// </summary>
        public string Title { get; init; } = string.Empty;

        /// <summary>
        /// A detailed description of the issue.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// The ID of the stakeholder this issue relates to.
        /// </summary>
        public Guid StakeholderId { get; init; }
    }
}

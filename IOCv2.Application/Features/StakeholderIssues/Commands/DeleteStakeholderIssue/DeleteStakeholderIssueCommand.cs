using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue
{
    /// <summary>
    /// Command to hard delete a stakeholder issue.
    /// </summary>
    public record DeleteStakeholderIssueCommand : IRequest<IOCv2.Application.Common.Models.Result<DeleteStakeholderIssueResponse>>
    {
        /// <summary>
        /// The ID of the stakeholder issue to delete.
        /// </summary>
        public Guid Id { get; init; }
    }
}

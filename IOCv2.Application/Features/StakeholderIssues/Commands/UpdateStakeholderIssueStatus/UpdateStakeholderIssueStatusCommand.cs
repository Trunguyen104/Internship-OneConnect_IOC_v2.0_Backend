using IOCv2.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus
{
    /// <summary>
    /// Command to update the status of an existing stakeholder issue.
    /// </summary>
    public record UpdateStakeholderIssueStatusCommand : IRequest<IOCv2.Application.Common.Models.Result<UpdateStakeholderIssueStatusResponse>>
    {
        /// <summary>
        /// The ID of the stakeholder issue to update.
        /// </summary>
        [JsonIgnore]
        public Guid Id { get; init; }

        /// <summary>
        /// The new status for the issue (Open, InProgress, Resolved, Closed).
        /// </summary>
        public string Status { get; init; } = string.Empty;
    }
}

using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder
{
    /// <summary>
    /// Command to soft delete a stakeholder.
    /// </summary>
    public record DeleteStakeholderCommand : IRequest<Result<DeleteStakeholderResponse>>
    {
        /// <summary>
        /// The ID of the stakeholder to delete.
        /// </summary>
        public Guid StakeholderId { get; init; }

        /// <summary>
        /// The ID of the internship group.
        /// </summary>
        public Guid InternshipId { get; init; }
    }
}

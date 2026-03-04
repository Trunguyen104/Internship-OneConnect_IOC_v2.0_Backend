using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById
{
    /// <summary>
    /// Query to get a single stakeholder issue by ID.
    /// </summary>
    public record GetStakeholderIssueByIdQuery : IRequest<IOCv2.Application.Common.Models.Result<GetStakeholderIssueByIdResponse>>
    {
        /// <summary>
        /// The ID of the stakeholder issue to retrieve.
        /// </summary>
        public Guid Id { get; init; }

        public GetStakeholderIssueByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}

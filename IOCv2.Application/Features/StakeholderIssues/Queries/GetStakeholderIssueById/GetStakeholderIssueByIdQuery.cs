using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById
{
    public record GetStakeholderIssueByIdQuery(Guid Id) : IRequest<Result<GetStakeholderIssueByIdResponse>>;
}


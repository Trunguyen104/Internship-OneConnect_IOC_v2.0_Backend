using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StakeholderIssues.DTOs;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById;

public record GetStakeholderIssueByIdQuery(Guid Id) : IRequest<Result<StakeholderIssueDto>>;

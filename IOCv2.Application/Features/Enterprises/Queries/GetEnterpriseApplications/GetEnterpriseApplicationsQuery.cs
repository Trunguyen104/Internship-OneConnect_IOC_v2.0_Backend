using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseApplications;

public record GetEnterpriseApplicationsQuery : IRequest<Result<PaginatedResult<GetEnterpriseApplicationsResponse>>>
{
    public Guid TermId { get; init; }
    public int PageIndex { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public bool? MentorAssigned { get; init; }
}

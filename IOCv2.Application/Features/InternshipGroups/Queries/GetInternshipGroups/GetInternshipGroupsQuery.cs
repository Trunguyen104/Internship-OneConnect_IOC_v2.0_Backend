using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups
{
    public record GetInternshipGroupsQuery : IRequest<Result<PaginatedResult<GetInternshipGroupsResponse>>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SearchTerm { get; init; }
        public short? Status { get; init; }
    }
}

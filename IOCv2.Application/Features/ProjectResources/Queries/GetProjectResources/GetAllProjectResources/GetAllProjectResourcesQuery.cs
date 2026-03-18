using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources
{
    public record GetAllProjectResourcesQuery : IRequest<Result<PaginatedResult<GetAllProjectResourcesResponse>>>
    {
        // Filters
        public Guid? ProjectId { get; init; }
        public FileType? ResourceType { get; init; }
        public string? SearchTerm { get; init; }

        // Pagination
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        // Sorting
        public string? SortColumn { get; init; }
        public string? SortOrder { get; init; }
    }
}

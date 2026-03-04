using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources
{
    public record GetAllProjectResourcesQuery : IRequest<Result<PaginatedResult<GetAllProjectResourcesResponse>>>
    {
        // Filters
        public Guid? ProjectId { get; init; }
        /// <summary>Type of file: DocumentFile, Image, Video, Other</summary>
        public string? ResourceType { get; init; }
        public string? SearchTerm { get; init; }

        // Pagination
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        // Sorting
        public string? SortColumn { get; init; }
        public string? SortOrder { get; init; }
    }
}

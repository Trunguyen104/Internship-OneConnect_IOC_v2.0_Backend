using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources
{
    public class GetAllProjectResourcesQuery : IRequest<Result<PaginatedResult<GetAllProjectResourcesResponse>>>
    {
        // Filters
        public Guid? ProjectId { get; set; }
        public FileType? ResourceType { get; set; }
        public string? SearchTerm { get; set; }

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Sorting
        public string? SortColumn { get; set; }
        public string? SortOrder { get; set; }
    }
}

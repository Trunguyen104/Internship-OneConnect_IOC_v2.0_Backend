using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetAProjects
{
    public class GetAllProjectsQuery : IRequest<Result<PaginatedResult<GetAllProjectsResponse>>>
    {
        // Search
        public string? SearchTerm { get; set; }

        // Filters
        public ProjectStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? InternshipId { get; set; }
        public Guid? StudentId { get; set; }

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Sorting
        public string? SortColumn { get; set; }
        public string? SortOrder { get; set; }
    }
}

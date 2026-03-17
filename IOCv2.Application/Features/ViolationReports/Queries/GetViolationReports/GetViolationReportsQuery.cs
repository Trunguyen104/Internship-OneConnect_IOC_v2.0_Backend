using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports
{
    public record GetViolationReportsQuery : IRequest<Result<PaginatedResult<GetViolationReportsResponse>>>
    {
        // Search
        public string? SearchTerm { get; set; }

        // Filters
        public Guid? CreatedById { get; set; }         
        public DateOnly? OccurredFrom { get; set; }     
        public DateOnly? OccurredTo { get; set; }      
        public Guid? GroupId { get; set; }            

        // Pagination
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Sorting: default newest first (CreatedAt desc). Client can toggle this.
        public bool OrderByCreatedAscending { get; set; } = false;
    }
}

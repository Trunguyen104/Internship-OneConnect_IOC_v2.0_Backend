using IOCv2.Application.Common.Models;
using System;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobs
{
    public record GetJobsQuery : MediatR.IRequest<Result<PaginatedResult<GetJobsResponse>>>
    {
        /// <summary>
        /// Search by job title or company name.
        /// </summary>
        public string? SearchTerm { get; init; }

        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        /// <summary>
        /// Column name used to sort results (e.g. title, expireDate).
        /// </summary>
        public string? SortColumn { get; init; }

        /// <summary>
        /// 'asc' or 'desc'
        /// </summary>
        public string? SortOrder { get; init; }
    }
}
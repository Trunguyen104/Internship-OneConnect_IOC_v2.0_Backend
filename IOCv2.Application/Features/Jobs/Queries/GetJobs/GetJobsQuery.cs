using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobs
{
    /// <summary>
    /// Query to retrieve a paginated list of jobs (student + HR views).
    /// </summary>
    public record GetJobsQuery : IRequest<Result<PaginatedResult<GetJobsResponse>>>
    {
        /// <summary>
        /// Search by job title or company name.
        /// </summary>
        public string? SearchTerm { get; init; }

        /// <summary>
        /// Filter by Job status (Draft / Published / Closed).
        /// Only applied for HR / Enterprise view.
        /// </summary>
        public JobStatus? Status { get; init; }

        /// <summary>
        /// Include jobs with status == Deleted when true. Default is false (keep UI clean).
        /// </summary>
        public bool IncludeDeleted { get; init; } = false;

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
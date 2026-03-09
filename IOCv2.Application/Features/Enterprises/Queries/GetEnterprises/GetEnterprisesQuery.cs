using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using Microsoft.AspNetCore.Http.HttpResults;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterprises
{
    public record GetEnterprisesQuery : MediatR.IRequest<Result<PaginatedResult<GetEnterprisesResponse>>>
    {
        /// <summary>
        /// Text input to search in Name, TaxCode, Industry, Description, Address, or Website.
        /// </summary>
        public string? SearchTerm { get; init; }

        /// <summary>
        /// Filter by enterprise tax code.
        /// </summary>
        public string? TaxCode { get; init; }

        /// <summary>
        /// Filter by enterprise name.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// Filter by industry.
        /// </summary>
        public string? Industry { get; init; }

        /// <summary>
        /// Filter by verification status.
        /// </summary>
        public bool? IsVerified { get; init; }

        /// <summary>
        /// Filter by enterprise status (Inactive, Active, Suspended).
        /// </summary>
        public EnterpriseStatus? Status { get; init; }

        /// <summary>
        /// Current page index (starts at 1).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of records per page.
        /// </summary>
        public int PageSize { get; init; } = 10;

        /// <summary>
        /// Column name used to sort the results.
        /// Example: name, taxCode, industry, status.
        /// </summary>
        public string? SortColumn { get; init; }

        /// <summary>
        /// Sorting direction ('asc' or 'desc').
        /// </summary>
        public string? SortOrder { get; init; }
    }
}

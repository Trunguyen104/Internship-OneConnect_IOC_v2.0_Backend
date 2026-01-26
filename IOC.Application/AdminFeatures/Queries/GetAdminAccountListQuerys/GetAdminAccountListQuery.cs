using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOC.Application.AdminFeatures.DTOs;
using IOC.Application.Commons.Models.Paging;
using IOC.Domain.Enums;
using MediatR;

namespace IOC.Application.AdminFeatures.Queries.GetAdminAccountListQuerys
{
    public class GetAdminAccountListQuery
        : IRequest<PagedResult<AdminAccountListDto>>
    {
        public string? Keyword { get; init; }

        // Advanced filter
        public string? Email { get; init; }
        public string? Code { get; init; }
        public Guid? OrganizationId { get; init; }
        public AdminRole? Role { get; init; }
        public AccountStatus? Status { get; init; }
        public DateTime? CreatedFrom { get; init; } 
        public DateTime? CreatedTo { get; init; }

        // Sort
        public string SortBy { get; init; } = "CreatedAt";     // FullName | CreatedAt
        public string SortDir { get; init; } = "asc";   // asc | desc

        // Paging
        public int PageIndex { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }

}

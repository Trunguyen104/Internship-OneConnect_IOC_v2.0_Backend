using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Admin.Users.Queries.GetAdminUsers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    public record GetLogbooksQuery : IRequest<Result<PaginatedResult<GetLogbooksResponse>>>
    {
        [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever]
        [System.Text.Json.Serialization.JsonIgnore]
        public Guid ProjectId { get; set; }
        public string? Status { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;

        public string? SortColumn { get; init; }
        public string? SortOrder { get; init; } // "asc" or "desc"
    }
}

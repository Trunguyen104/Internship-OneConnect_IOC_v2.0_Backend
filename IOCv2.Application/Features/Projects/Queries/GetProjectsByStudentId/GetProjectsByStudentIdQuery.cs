using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId
{
    public record GetProjectsByStudentIdQuery : IRequest<Result<PaginatedResult<GetProjectsByStudentIdResponse>>>
    {
        public string? SearchTerm { get; init; }
        public ProjectStatus? Status { get; init; }
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SortColumn { get; init; }
        public string? SortOrder { get; init; }
    }

}

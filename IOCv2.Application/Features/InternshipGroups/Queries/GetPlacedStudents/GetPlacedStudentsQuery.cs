using IOCv2.Application.Common.Models;
using MediatR;
using System;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetPlacedStudents
{
    public record GetPlacedStudentsQuery : IRequest<Result<PaginatedResult<GetPlacedStudentsResponse>>>
    {
        public Guid? PhaseId { get; init; }     // null = tự động tìm phase Active/Upcoming
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SearchTerm { get; init; }
        public bool? IsAssignedToGroup { get; init; }
    }
}

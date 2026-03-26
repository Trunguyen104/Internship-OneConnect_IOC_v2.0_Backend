using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Jobs.Queries.GetAllJobApplications
{
    public record GetAllInternshipApplicationsQuery : IRequest<Result<PaginatedResult<GetAllInternshipApplicationResponse>>>
    {
        public Guid? JobId { get; set; }
        public JobStatus? JobStatus { get; set; }
        public InternshipApplicationStatus? ApplicationStatus { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? SortColumn { get; set; }
        public string? SortOrder { get; set; }
    }
}
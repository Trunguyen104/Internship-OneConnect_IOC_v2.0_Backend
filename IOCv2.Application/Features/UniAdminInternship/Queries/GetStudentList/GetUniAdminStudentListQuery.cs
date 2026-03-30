using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAdminInternship.Common;
using MediatR;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentList;

public record GetUniAdminStudentListQuery : IRequest<Result<GetUniAdminStudentListResponse>>
{
    public Guid? TermId { get; init; }
    public string? SearchTerm { get; init; }
    public Guid? EnterpriseId { get; init; }
    public InternshipUiStatus? Status { get; init; }
    public LogbookFilterStatus? LogbookStatus { get; init; }
    public string SortBy { get; init; } = "fullname";
    public string SortOrder { get; init; } = "asc";
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

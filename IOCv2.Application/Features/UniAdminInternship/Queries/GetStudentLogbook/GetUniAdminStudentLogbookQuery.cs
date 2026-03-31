using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbook;

public record GetUniAdminStudentLogbookQuery : IRequest<Result<GetUniAdminStudentLogbookResponse>>
{
    public Guid StudentId { get; init; }
    public Guid? TermId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 4;
}


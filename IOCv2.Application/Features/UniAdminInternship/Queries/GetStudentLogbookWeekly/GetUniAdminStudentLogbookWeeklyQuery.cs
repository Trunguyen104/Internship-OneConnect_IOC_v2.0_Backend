using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbookWeekly;

public record GetUniAdminStudentLogbookWeeklyQuery : IRequest<Result<GetUniAdminStudentLogbookWeeklyResponse>>
{
    public Guid StudentId { get; init; }
    public Guid? TermId { get; init; }
}


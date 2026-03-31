using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbookTotal;

public record GetUniAdminStudentLogbookTotalQuery : IRequest<Result<GetUniAdminStudentLogbookTotalResponse>>
{
    public Guid StudentId { get; init; }
    public Guid? TermId { get; init; }
}


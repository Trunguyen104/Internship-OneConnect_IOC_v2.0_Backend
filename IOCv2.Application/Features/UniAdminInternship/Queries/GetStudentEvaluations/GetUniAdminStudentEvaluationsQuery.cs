using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentEvaluations;

public record GetUniAdminStudentEvaluationsQuery : IRequest<Result<GetUniAdminStudentEvaluationsResponse>>
{
    public Guid StudentId { get; init; }
    public Guid? TermId { get; init; }
}

using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentViolations;

public record GetUniAdminStudentViolationsQuery : IRequest<Result<GetUniAdminStudentViolationsResponse>>
{
    public Guid StudentId { get; init; }
    public Guid? TermId { get; init; }
}

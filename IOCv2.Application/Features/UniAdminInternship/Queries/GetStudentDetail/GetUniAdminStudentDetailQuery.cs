using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentDetail;

public record GetUniAdminStudentDetailQuery : IRequest<Result<GetUniAdminStudentDetailResponse>>
{
    public Guid StudentId { get; init; }
    public Guid? TermId { get; init; }
}

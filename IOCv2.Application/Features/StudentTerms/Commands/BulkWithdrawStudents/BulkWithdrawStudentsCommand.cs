using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;

public record BulkWithdrawStudentsCommand : IRequest<Result<BulkWithdrawStudentsResponse>>
{
    public Guid TermId { get; init; }
    public List<Guid> StudentTermIds { get; init; } = new();
}

using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;

public record BulkWithdrawStudentsCommand : IRequest<Result<BulkWithdrawStudentsResponse>>
{
    public Guid TermId { get; init; }
    public List<Guid> StudentTermIds { get; init; } = new();
}

public class BulkWithdrawStudentsResponse
{
    public int WithdrawnCount { get; set; }
    public int DeletedFromSystemCount { get; set; }
    public int SkippedPlacedCount { get; set; }
    public int SkippedAlreadyWithdrawnCount { get; set; }
    public int SkippedHasOtherTermsCount { get; set; }
}

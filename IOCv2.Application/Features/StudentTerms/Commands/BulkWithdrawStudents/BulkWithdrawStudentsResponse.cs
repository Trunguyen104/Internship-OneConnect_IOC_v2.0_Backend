namespace IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;

public record BulkWithdrawStudentsResponse
{
    public int WithdrawnCount { get; init; }
    public int SkippedPlacedCount { get; init; }
    public int SkippedAlreadyWithdrawnCount { get; init; }
}

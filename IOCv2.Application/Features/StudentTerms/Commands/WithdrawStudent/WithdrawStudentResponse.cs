namespace IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;

public record WithdrawStudentResponse
{
    public Guid StudentTermId { get; init; }
}

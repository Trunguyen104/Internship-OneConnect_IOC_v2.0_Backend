using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;

public record WithdrawStudentCommand(Guid StudentTermId) : IRequest<Result<WithdrawStudentResponse>>;

public class WithdrawStudentResponse
{
    public Guid StudentTermId { get; set; }
}

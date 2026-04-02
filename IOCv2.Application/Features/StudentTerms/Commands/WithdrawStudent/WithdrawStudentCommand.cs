using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;

public record WithdrawStudentCommand(Guid StudentTermId, bool DeleteFromSystem = false)
    : IRequest<Result<WithdrawStudentResponse>>;

public class WithdrawStudentResponse
{
    public Guid StudentTermId { get; set; }
    public bool StudentDeletedFromSystem { get; set; }
    public int SystemStudentDelta { get; set; }
    public string? UiWarningMessageKey { get; set; }
    public string? UiWarningMessage { get; set; }
}

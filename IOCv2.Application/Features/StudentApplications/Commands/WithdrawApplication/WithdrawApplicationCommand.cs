using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentApplications.Commands.WithdrawApplication;

public record WithdrawApplicationCommand(Guid ApplicationId) : IRequest<Result<WithdrawApplicationResponse>>;

public class WithdrawApplicationResponse
{
    public Guid ApplicationId { get; set; }
    public string Message { get; set; } = string.Empty;
}

using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Authentication.Commands.ResetPassword
{
    public record ResetPasswordCommand
    (
        string Token,
        string NewPassword,
        string ConfirmPassword
    ) : IRequest<Result<string>>;
}

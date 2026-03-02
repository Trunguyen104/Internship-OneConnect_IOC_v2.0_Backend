using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand(string Email, string Password, bool RememberMe) : IRequest<Result<LoginResponse>>;
}

using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Authentication.Commands.Login;
using MediatR;

namespace IOCv2.Application.Features.Authentication.Commands.RefreshTokens
{
    public record RefreshTokenCommand(string RefreshToken) : IRequest<Result<LoginResponse>>
    {
    }
}

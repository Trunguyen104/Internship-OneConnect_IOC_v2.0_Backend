using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Authentication.Commands.Login
{
    /// <summary>
    /// Command to authenticate a user using email and password.
    /// </summary>
    /// <param name="Email">The registered email of the user.</param>
    /// <param name="Password">The password associated with the email.</param>
    /// <param name="RememberMe">Indicates whether to issue a long-lived refresh token.</param>
    public record LoginCommand(string Email, string Password, bool RememberMe) : IRequest<Result<LoginResponse>>;
}

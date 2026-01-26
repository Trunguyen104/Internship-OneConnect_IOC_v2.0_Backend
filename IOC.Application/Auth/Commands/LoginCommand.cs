using IOC.Application.Auth.DTOs;
using MediatR;

namespace IOC.Application.Auth.Commands
{
    // Login command carries credentials and returns LoginResultDto
    public class LoginCommand : IRequest<LoginResultDto>
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand(string Email, string Password, bool RememberMe) : IRequest<Result<LoginResponse>>;
}

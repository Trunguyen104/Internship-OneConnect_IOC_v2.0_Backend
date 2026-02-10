using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Authentication.Commands.Logout
{
    public class RevokeTokenCommand : IRequest<Result<bool>>
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}

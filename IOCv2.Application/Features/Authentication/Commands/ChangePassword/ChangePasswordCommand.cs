using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Authentication.Commands.ChangePassword
{
    public record ChangePasswordCommand : IRequest<Result<string>>
    {
        public string CurrentPassword { get; init; } = default!;
        public string NewPassword { get; init; } = default!;
        public string ConfirmPassword { get; init; } = default!;
    }
}

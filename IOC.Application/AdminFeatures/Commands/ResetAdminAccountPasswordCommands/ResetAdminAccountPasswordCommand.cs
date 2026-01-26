using MediatR;
using System;

namespace IOC.Application.AdminFeatures.Commands.ResetAdminAccountPasswordCommands
{
    public class ResetAdminAccountPasswordCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
    }
}

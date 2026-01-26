using MediatR;
using System;
using IOC.Domain.Enums;

namespace IOC.Application.AdminFeatures.Commands.UpdateAdminAccountStatusCommands
{
    public class UpdateAdminAccountStatusCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
        public AccountStatus TargetStatus { get; set; }
    }
}

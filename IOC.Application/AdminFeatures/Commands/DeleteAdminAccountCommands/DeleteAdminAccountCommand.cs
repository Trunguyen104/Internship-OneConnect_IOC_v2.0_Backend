using MediatR;
using System;

namespace IOC.Application.AdminFeatures.Commands.DeleteAdminAccountCommands
{
    public class DeleteAdminAccountCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
    }
}

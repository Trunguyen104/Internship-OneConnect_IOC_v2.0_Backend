using MediatR;
using System;
using IOC.Domain.Enums;

namespace IOC.Application.AdminFeatures.Commands.ChangeAdminAccountRoleCommands
{
    public class ChangeAdminAccountRoleCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
        public AdminRole Role { get; set; }
        public Guid? OrganizationId { get; set; }
    }
}

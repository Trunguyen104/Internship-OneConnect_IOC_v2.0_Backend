using IOC.Domain.Enums;
using MediatR;
using System;

namespace IOC.Application.AdminFeatures.Commands.UpdateAdminAccountCommands
{
    public class UpdateAdminAccountCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public AdminRole Role { get; set; }
        public Guid? OrganizationId { get; set; } = Guid.Empty;
        public string Code { get; set; }
    }
}

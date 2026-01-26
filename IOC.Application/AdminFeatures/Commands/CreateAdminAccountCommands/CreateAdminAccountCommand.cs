using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.Commands.CreateAdminAccountCommands
{
    using IOC.Domain.Enums;
    using MediatR;

    public class CreateAdminAccountCommand : IRequest<Guid>
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public AdminRole Role { get; set; }
        public Guid? OrganizationId { get; set; }
        public string Code { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
    }

}

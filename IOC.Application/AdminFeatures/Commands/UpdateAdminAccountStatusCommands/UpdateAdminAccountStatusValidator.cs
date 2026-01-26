using IOC.Domain.Enums;
using System;

namespace IOC.Application.AdminFeatures.Commands.UpdateAdminAccountStatusCommands
{
    public class UpdateAdminAccountStatusValidator
    {
        public void Validate(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid account id.");
        }
    }
}

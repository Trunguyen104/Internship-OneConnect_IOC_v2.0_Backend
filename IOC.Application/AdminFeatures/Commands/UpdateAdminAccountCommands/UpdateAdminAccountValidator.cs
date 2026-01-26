using System;
using System.Text.RegularExpressions;
using IOC.Domain.Enums;

namespace IOC.Application.AdminFeatures.Commands.UpdateAdminAccountCommands
{
    public class UpdateAdminAccountValidator
    {
        public void Validate(UpdateAdminAccountCommand command)
        {
            InputId(command.Id);
            InputName(command.FullName);
            InputRole(command.Role);
            InputOrganizationId(command.OrganizationId, command.Role);
            InputCode(command.Code);
            if (command == null) throw new ArgumentNullException(nameof(command));
        }

        public void InputId(Guid id)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Invalid account id.");
        }

        public void InputName(string FullName)
        {
            if (string.IsNullOrWhiteSpace(FullName))
                throw new ArgumentException("Full Name cannot be left blank");

            if (!Regex.IsMatch(FullName.Trim(), @"^[\p{L} .'-]+$"))
                throw new ArgumentException("Full name may only contain letters, spaces, '.', '\'' and '-'.");

            if (FullName.Trim().Length < 2)
                throw new ArgumentException("Full name must be at least 2 characters long.");
        }

        public void InputRole(AdminRole role)
        {
            if (!Enum.IsDefined(typeof(AdminRole), role))
                throw new ArgumentException("Invalid role.");
        }

        public void InputOrganizationId(Guid? organizationId, AdminRole role)
        {
            if (role == AdminRole.School || role == AdminRole.Enterprise)
            {
                if (!organizationId.HasValue || organizationId == Guid.Empty)
                    throw new ArgumentException("OrganizationId is required for School and Enterprise roles.");
            }
            else if (role == AdminRole.Internal)
            {
                // Internal must not have organization
                if (organizationId.HasValue && organizationId != Guid.Empty)
                    throw new ArgumentException("Internal role must not have an organization.");
            }
        }

        public void InputCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            if (!Regex.IsMatch(code, @"^[A-Za-z0-9_-]{3,50}$"))
                throw new ArgumentException("Code must be alphanumeric (3-50 characters) and may include '_' or '-'.");
        }
    }
}

using System;
using System.Text.RegularExpressions;
using IOC.Domain.Enums;

namespace IOC.Application.AdminFeatures.Commands.CreateAdminAccountCommands
{
    public class CreateAdminAccountValidator
    {
        public CreateAdminAccountValidator() { }

        public bool Validate(CreateAdminAccountCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            InputName(command.FullName);
            InputEmail(command.Email);
            InputRole(command.Role);
            InputOrganizationId(command.OrganizationId, command.Role);
            InputCode(command.Code);
            InputPassword(command.Password, command.ConfirmPassword);
            return true;
        }

        public void InputName(string FullName)
        {
            if (string.IsNullOrWhiteSpace(FullName))
                throw new ArgumentException("Full Name cannot be left blank");

            // Allow Unicode letters, spaces, apostrophes, dots and hyphens
            if (!Regex.IsMatch(FullName.Trim(), @"^[\p{L} .'-]+$"))
                throw new ArgumentException("Full name may only contain letters, spaces, '.', '\'' and '-'.");

            if (FullName.Trim().Length < 2)
                throw new ArgumentException("Full name must be at least 2 characters long.");
        }

        public void InputEmail(string Email)
        {
            if (string.IsNullOrWhiteSpace(Email))
                throw new ArgumentException("Email cannot be left blank");

            if (!Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                throw new ArgumentException("Invalid email format.");
        }

        public void InputRole(AdminRole role)
        {
            if (!Enum.IsDefined(typeof(AdminRole), role))
                throw new ArgumentException("Invalid role.");
        }

        public void InputOrganizationId(Guid? organizationId, AdminRole role)
        {
            // For School and Enterprise roles an OrganizationId is required
            if (role == AdminRole.School || role == AdminRole.Enterprise)
            {
                if (!organizationId.HasValue || organizationId == Guid.Empty)
                    throw new ArgumentException("OrganizationId is required for School and Enterprise roles.");
            }
        }

        public void InputCode(string code)
        {
            // Code is optional but if provided, validate format (alphanumeric, 3-50 chars)
            if (string.IsNullOrWhiteSpace(code))
                return;

            if (!Regex.IsMatch(code, @"^[A-Za-z0-9_-]{3,50}$"))
                throw new ArgumentException("Code must be alphanumeric (3-50 characters) and may include '_' or '-'.");
        }

        public void InputPassword(string password, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be left blank");

            if (password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters long.");

            // Require at least one uppercase, lowercase, digit and special character
            if (!Regex.IsMatch(password, @"[A-Z]+"))
                throw new ArgumentException("Password must contain at least one uppercase letter.");

            if (!Regex.IsMatch(password, @"[a-z]+"))
                throw new ArgumentException("Password must contain at least one lowercase letter.");

            if (!Regex.IsMatch(password, @"[0-9]+"))
                throw new ArgumentException("Password must contain at least one digit.");

            // special characters set
            if (!Regex.IsMatch(password, @"[!@#$%^&*()_+\-=[\]{};':""\\|,.<>\/\?]+"))
                throw new ArgumentException("Password must contain at least one special character.");

            if (password != confirmPassword)
                throw new ArgumentException("Password and ConfirmPassword do not match.");
        }
    }
}

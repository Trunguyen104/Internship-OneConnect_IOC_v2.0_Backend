using IOC.Domain.Enums;
using IOC.Domain.Exceptions;
using IOC.Domain.Extensions;
using IOC.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOC.Domain.Entities
{
    public class AdminAccount
    {
        public Guid Id { get; private set; }
        public string Code { get; private set; }
        public string FullName { get; private set; }
        public Email Email { get; private set; }
        public AdminRole Role { get; private set; }
        public Guid? OrganizationId { get; private set; }
        public string PasswordHash { get; private set; }
        public AccountStatus Status { get; private set; }
        public DateTime CreatedAt { get; private set; }

        private AdminAccount() { }

        public AdminAccount(Email email) {
            Email = email;
        }

        public void ChangeEmail(Email newEmail)
        {
            Email = newEmail;
        }

        public static AdminAccount Update(
            AdminAccount existingAccount,
            string fullName,
            AdminRole role,
            Guid? organizationId,
            string code)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new DomainException("Full Name cannot be left blank");
            if (role.RequiresRole() && organizationId == null)
                throw new DomainException("This role requires the organization");
            if (!role.RequiresRole() && organizationId != null)
                throw new DomainException("This role has not been assigned a organization");
            if (role == AdminRole.Internal && organizationId != null)
                throw new DomainException("INTERNAL has no organization");
            if (role != AdminRole.Internal && organizationId == null && role != AdminRole.Master)
                throw new DomainException("Lack of organization");
            existingAccount.FullName = fullName.Trim();
            existingAccount.Role = role;
            existingAccount.OrganizationId = organizationId;
            existingAccount.Code = code;
            return existingAccount;
        }

        public static AdminAccount Create(
            string fullName,
            Email email,
            AdminRole role,
            Guid? organizationId,
            string passwordHash,
            string code)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new DomainException("Full Name cannot be left blank");

            if (role.RequiresRole() && organizationId == null)
                throw new DomainException( "This role requires the organization");

            if (!role.RequiresRole() && organizationId != null)
                throw new DomainException( "This role has not been assigned a organization");

            if (role == AdminRole.Internal && organizationId != null)
                throw new DomainException( "INTERNAL has no organization");

            if (role != AdminRole.Internal && organizationId == null && role != AdminRole.Master)
                throw new DomainException("Lack of organization");

            return new AdminAccount
            {
                Id = Guid.NewGuid(),
                FullName = fullName.Trim(),
                Email = email,
                Role = role,
                OrganizationId = organizationId,
                PasswordHash = passwordHash,
                Code = code,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void SetStatus(AccountStatus status)
        {
            // You can add business rules here (e.g., cannot set Banned directly)
            Status = status;
        }

        public void SetPasswordHash(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash cannot be empty.");
            PasswordHash = passwordHash;
        }

        public void ChangeRole(AdminRole role, Guid? organizationId)
        {
            // Reuse the same validation rules as Update/Create
            if (role.RequiresRole() && organizationId == null)
                throw new DomainException("This role requires the organization");
            if (!role.RequiresRole() && organizationId != null)
                throw new DomainException("This role has not been assigned a organization");
            if (role == AdminRole.Internal && organizationId != null)
                throw new DomainException("INTERNAL has no organization");
            if (role != AdminRole.Internal && organizationId == null && role != AdminRole.Master)
                throw new DomainException("Lack of organization");

            Role = role;
            OrganizationId = organizationId;
        }

        public virtual Organization? Organization { get; private set; }
    }

}

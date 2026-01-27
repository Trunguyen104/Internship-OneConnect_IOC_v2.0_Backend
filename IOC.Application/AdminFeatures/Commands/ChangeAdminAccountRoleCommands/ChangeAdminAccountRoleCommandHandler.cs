using IOC.Domain.Exceptions;
using IOC.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Application.Commons.Interfaces.Services;

namespace IOC.Application.AdminFeatures.Commands.ChangeAdminAccountRoleCommands
{
    public class ChangeAdminAccountRoleCommandHandler : IRequestHandler<ChangeAdminAccountRoleCommand, Guid>
    {
        private readonly IAdminAccountRepository _repository;
        private readonly IAuditLogService _auditLog;
        private readonly ICurrentUserService _currentUser;

        public ChangeAdminAccountRoleCommandHandler(
            IAdminAccountRepository repository,
            IAuditLogService auditLog,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _auditLog = auditLog;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Handle a request to change an administrator's role.
        /// Steps:
        /// 1. Validate the request payload.
        /// 2. Load the target account.
        /// 3. Enforce permission rules (only MASTER can assign MASTER).
        /// 4. Prevent administrators from changing their own role.
        /// 5. Short-circuit when there is no effective change.
        /// 6. Ensure the system never removes the last MASTER account.
        /// 7. Apply change via domain method and persist.
        /// 8. Emit an audit log entry.
        /// </summary>
        public async Task<Guid> Handle(ChangeAdminAccountRoleCommand request, CancellationToken cancellationToken)
        {
            // Validate input
            if (request == null || request.Id == Guid.Empty)
                throw new ArgumentException("Invalid request.");

            // Retrieve the target admin account
            var account = await _repository.GetByIdAsync(request.Id);
            if (account == null)
                throw new DomainException("Admin account not found.");

            // Only a MASTER admin can assign the MASTER role to someone
            if (request.Role == AdminRole.Master && _currentUser.Role != AdminRole.Master)
                throw new DomainException("Only MASTER administrators can assign the MASTER role.");

            // Prevent administrators from changing their own role (safety / privilege escalation)
            if (account.Id == _currentUser.UserId)
                throw new DomainException("Administrators cannot change their own role.");

            // If requested role and organization are identical to current, nothing to do
            if (account.Role == request.Role &&
                account.OrganizationId == request.OrganizationId)
            {
                // No change needed
                return account.Id;
            }

            // If removing MASTER role from this account, ensure at least one MASTER remains
            if (account.Role == AdminRole.Master && request.Role != AdminRole.Master)
            {
                // Count current MASTER accounts — defend against removing the last MASTER
                var masters = await _repository.CountByRoleAsync(AdminRole.Master);
                if (masters <= 1)
                    throw new DomainException("Cannot remove MASTER role from the last MASTER account.");
            }

            // Apply role change through domain entity to ensure invariants are preserved
            account.ChangeRole(request.Role, request.OrganizationId);

            // Persist changes
            await _repository.UpdateAsync(account);

            // Record audit log for the role change (actor, action, target, description)
            await _auditLog.LogAsync(
                _currentUser.UserId,
                "CHANGE_ADMIN_ACCOUNT_ROLE",
                account.Id,
                $"Changed role for administrative account: {account.Email} to {request.Role}",
                cancellationToken
            );

            return account.Id;
        }
    }
}

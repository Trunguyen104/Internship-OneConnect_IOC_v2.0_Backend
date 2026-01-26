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

        public ChangeAdminAccountRoleCommandHandler(
            IAdminAccountRepository repository,
            IAuditLogService auditLog)
        {
            _repository = repository;
            _auditLog = auditLog;
        }

        public async Task<Guid> Handle(ChangeAdminAccountRoleCommand request, CancellationToken cancellationToken)
        {
            if (request == null || request.Id == Guid.Empty)
                throw new ArgumentException("Invalid request.");

            var account = await _repository.GetByIdAsync(request.Id);
            if (account == null)
                throw new DomainException("Admin account not found.");

            // If removing MASTER role, ensure at least one master remains
            if (account.Role == AdminRole.Master && request.Role != AdminRole.Master)
            {
                var masters = await _repository.CountByRoleAsync(AdminRole.Master);
                if (masters <= 1)
                    throw new DomainException("Cannot remove MASTER role from the last MASTER account.");
            }

            // Apply role change via domain method
            account.ChangeRole(request.Role, request.OrganizationId);

            await _repository.UpdateAsync(account);

            await _auditLog.LogAsync(
                "CHANGE_ADMIN_ACCOUNT_ROLE",
                account.Id,
                $"Changed role for administrative account: {account.Email} to {request.Role}",
                cancellationToken
            );

            return account.Id;
        }
    }
}

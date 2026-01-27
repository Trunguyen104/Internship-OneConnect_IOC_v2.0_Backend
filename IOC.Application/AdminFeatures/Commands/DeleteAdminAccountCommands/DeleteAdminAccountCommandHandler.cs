using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Application.Commons.Interfaces.Services;
using IOC.Domain.Exceptions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.Commands.DeleteAdminAccountCommands
{
    public class DeleteAdminAccountCommandHandler : IRequestHandler<DeleteAdminAccountCommand, Guid>
    {
        private readonly IAdminAccountRepository _repository;
        private readonly IAuditLogService _auditLog;
        private readonly ICurrentUserService _currentUser;

        public DeleteAdminAccountCommandHandler(
            IAdminAccountRepository repository,
            IAuditLogService auditLog,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _auditLog = auditLog;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(DeleteAdminAccountCommand request, CancellationToken cancellationToken)
        {
            if (request == null || request.Id == Guid.Empty)
                throw new ArgumentException("Invalid request.");

            var account = await _repository.GetByIdAsync(request.Id);
            if (account == null)
                throw new DomainException("Admin account not found.");

            if (account.Id == _currentUser.UserId)
                throw new DomainException("Administrators cannot delete their own account.");

            if (_currentUser.Role != Domain.Enums.AdminRole.Master && account.Role == Domain.Enums.AdminRole.Master)
                throw new DomainException("Only MASTER administrators can delete MASTER accounts.");

            if (account.Role == Domain.Enums.AdminRole.Master)
            {
                var masters = await _repository.CountByRoleAsync(Domain.Enums.AdminRole.Master);
                if (masters <= 1)
                    throw new DomainException("Cannot delete the last MASTER admin account.");
            }

            try
            {
                await _repository.DeleteAsync(account);
            }
            catch (Exception ex)
            {
                // If it's a foreign key / constraint issue bubble up a domain specific exception
                throw new DomainException("Cannot delete admin account due to linked data.");
            }

            await _auditLog.LogAsync(
                _currentUser.UserId,
                "DELETE_ADMIN_ACCOUNT",
                account.Id,
                $"Deleted administrative account: {account.Email}",
                cancellationToken
            );

            return account.Id;
        }
    }
}

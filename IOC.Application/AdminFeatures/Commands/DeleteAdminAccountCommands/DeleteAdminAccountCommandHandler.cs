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

        public DeleteAdminAccountCommandHandler(
            IAdminAccountRepository repository,
            IAuditLogService auditLog)
        {
            _repository = repository;
            _auditLog = auditLog;
        }

        public async Task<Guid> Handle(DeleteAdminAccountCommand request, CancellationToken cancellationToken)
        {
            if (request == null || request.Id == Guid.Empty)
                throw new ArgumentException("Invalid request.");

            var account = await _repository.GetByIdAsync(request.Id);
            if (account == null)
                throw new DomainException("Admin account not found.");

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
                "DELETE_ADMIN_ACCOUNT",
                account.Id,
                $"Deleted administrative account: {account.Email}",
                cancellationToken
            );

            return account.Id;
        }
    }
}

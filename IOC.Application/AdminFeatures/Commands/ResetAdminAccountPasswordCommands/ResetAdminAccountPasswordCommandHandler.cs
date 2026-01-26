using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Application.Commons.Interfaces.Services;
using IOC.Domain.Exceptions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.Commands.ResetAdminAccountPasswordCommands
{
    public class ResetAdminAccountPasswordCommandHandler : IRequestHandler<ResetAdminAccountPasswordCommand, Guid>
    {
        private readonly IAdminAccountRepository _repository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogService _auditLog;

        public ResetAdminAccountPasswordCommandHandler(
            IAdminAccountRepository repository,
            IPasswordHasher passwordHasher,
            IAuditLogService auditLog)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _auditLog = auditLog;
        }

        public async Task<Guid> Handle(ResetAdminAccountPasswordCommand request, CancellationToken cancellationToken)
        {
            if (request == null || request.Id == Guid.Empty)
                throw new ArgumentException("Invalid request.");

            var account = await _repository.GetByIdAsync(request.Id);
            if (account == null)
                throw new DomainException("Admin account not found.");

            // Reset to a default secure password. In production, you'd generate a random password and email it.
            var defaultPassword = "ChangeMe@123"; // TODO: replace with secure generator
            var hashed = _passwordHasher.Hash(defaultPassword);

            account.SetPasswordHash(hashed);

            await _repository.UpdateAsync(account);

            await _auditLog.LogAsync(
                "RESET_ADMIN_ACCOUNT_PASSWORD",
                account.Id,
                $"Reset administrative account password: {account.Email}",
                cancellationToken
            );

            return account.Id;
        }
    }
}

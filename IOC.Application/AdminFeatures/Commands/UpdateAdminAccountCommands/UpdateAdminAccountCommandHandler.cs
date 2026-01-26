using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Application.Commons.Interfaces.Services;
using IOC.Domain.Entities;
using IOC.Domain.Enums;
using IOC.Domain.Exceptions;
using IOC.Domain.ValueObjects;
using MediatR;
using System;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.Commands.UpdateAdminAccountCommands
{
    public class UpdateAdminAccountCommandHandler : IRequestHandler<UpdateAdminAccountCommand, Guid>
    {
        private readonly IAdminAccountRepository _repository;
        private readonly IAuditLogService _auditLog;

        public UpdateAdminAccountCommandHandler(
            IAdminAccountRepository repository,
            IAuditLogService auditLog)
        {
            _repository = repository;
            _auditLog = auditLog;
        }

        public async Task<Guid> Handle(UpdateAdminAccountCommand request, CancellationToken cancellationToken)
        {
            var validator = new UpdateAdminAccountValidator();
            try
            {
                validator.Validate(request);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }

            var account = await _repository.GetByIdAsync(request.Id);
            if (account == null)
                throw new DomainException("Admin account not found.");

            AdminAccount.Update(
                account,
                request.FullName,
                request.Role,
                request.OrganizationId,
                request.Code
            );

            await _repository.UpdateAsync(account);

            await _auditLog.LogAsync(
                "UPDATE_ADMIN_ACCOUNT",
                account.Id,
                $"Updated administrative account: {account.Email}",
                cancellationToken
            );

            return account.Id;
        }
    }
}

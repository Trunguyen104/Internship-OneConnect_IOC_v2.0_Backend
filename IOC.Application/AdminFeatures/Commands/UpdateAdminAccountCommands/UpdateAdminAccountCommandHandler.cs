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
        private readonly ICurrentUserService _currentUser;

        public UpdateAdminAccountCommandHandler(
            IAdminAccountRepository repository,
            IAuditLogService auditLog,
            ICurrentUserService currentUser)
        {
            _repository = repository;
            _auditLog = auditLog;
            _currentUser = currentUser;
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

            if (request.Role == AdminRole.Master && _currentUser.Role != AdminRole.Master)
                throw new DomainException("Only MASTER administrators can assign the MASTER role.");



            AdminAccount.Update(
                account,
                request.FullName,
                request.Role,
                request.OrganizationId,
                request.Code
            );

            await _repository.UpdateAsync(account);

            await _auditLog.LogAsync(
                _currentUser.UserId,
                "UPDATE_ADMIN_ACCOUNT",
                account.Id,
                $"Updated administrative account: {account.Email}",
                cancellationToken
            );

            return account.Id;
        }
    }
}

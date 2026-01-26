using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Domain.Entities;
using IOC.Domain.Enums;
using IOC.Domain.Exceptions;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.Commands.UpdateAdminAccountStatusCommands
{
    public class UpdateAdminAccountStatusCommandHandler : IRequestHandler<UpdateAdminAccountStatusCommand, Guid>
    {
        private readonly IAdminAccountRepository _repository;

        public UpdateAdminAccountStatusCommandHandler(IAdminAccountRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(UpdateAdminAccountStatusCommand request, CancellationToken cancellationToken)
        {
            var validator = new UpdateAdminAccountStatusValidator();
            validator.Validate(request.Id);

            var account = await _repository.GetByIdAsync(request.Id);
            if (account == null)
                throw new DomainException("Admin account not found.");

            // If disabling, ensure at least one master remains
            if (request.TargetStatus == AccountStatus.Inactive && account.Role == AdminRole.Master)
            {
                var masters = await _repository.CountByRoleAsync(AdminRole.Master);
                if (masters <= 1)
                    throw new DomainException("Cannot disable the last MASTER account.");
            }

            // Update status via domain method
            account.SetStatus(request.TargetStatus);

            await _repository.UpdateAsync(account);

            return account.Id;
        }
    }
}

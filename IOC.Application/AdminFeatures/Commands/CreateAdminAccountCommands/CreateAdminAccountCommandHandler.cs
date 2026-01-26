using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Application.Commons.Interfaces.Services;
using IOC.Domain.Entities;
using IOC.Domain.Exceptions;
using IOC.Domain.ValueObjects;
using MediatR;

namespace IOC.Application.AdminFeatures.Commands.CreateAdminAccountCommands
{
    

    public class CreateAdminAccountCommandHandler
        : IRequestHandler<CreateAdminAccountCommand, Guid>
    {
        private readonly IAdminAccountRepository _repository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IAuditLogService _auditLog;

        public CreateAdminAccountCommandHandler(
            IAdminAccountRepository repository,
            IPasswordHasher passwordHasher,
            IAuditLogService auditLog)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _auditLog = auditLog;
        }

        public async Task<Guid> Handle(
            CreateAdminAccountCommand request,
            CancellationToken cancellationToken)
        {
            try 
            {
                var validator = new CreateAdminAccountValidator();
                validator.Validate(request);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message);
            }

            if (await _repository.ExistsByEmailAsync(request.Email)) ;

            var passwordHash = _passwordHasher.Hash(request.Password);

            var account = AdminAccount.Create(
                request.FullName,
                Email.Create(request.Email),
                request.Role,
                request.OrganizationId.Value,
                passwordHash,
                request.Code
            );

            await _repository.AddAsync(account);

            await _auditLog.LogAsync(
                "CREATE_ADMIN_ACCOUNT",
                account.Id,
                $"Create an administrative account: {account.Email}"
            );

            return account.Id;
        }
    }

}

using IOC.Application.Auth.DTOs;
using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Application.Commons.Interfaces.Services;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOC.Application.Auth.Commands
{
    // Handler performs authentication: find account, verify password, create token
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResultDto>
    {
        private readonly IAdminAccountRepository _repository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ITokenService _tokenService;

        public LoginCommandHandler(
            IAdminAccountRepository repository,
            IPasswordHasher passwordHasher,
            ITokenService tokenService)
        {
            _repository = repository;
            _passwordHasher = passwordHasher;
            _tokenService = tokenService;
        }

        public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrEmpty(request.Password))
                throw new ArgumentException("Email and password must be provided.");

            var account = await _repository.GetByEmailAsync(request.Email);
            if (account == null)
                throw new ArgumentException("Email or Password is incorrect.");

            // Verify password with the application's password hasher
            var valid = _passwordHasher.Verify(account.PasswordHash, request.Password);
            if (!valid)
                throw new ArgumentException("Email or Password is incorrect.");

            // Check if account is banned
            if (account.Status == Domain.Enums.AccountStatus.Banned)
                throw new ArgumentException("Account is banned.");

            // Check account status
            if (account.Status != Domain.Enums.AccountStatus.Active)
                throw new ArgumentException("Account is not active.");

            // Generate JWT
            var token = _tokenService.GenerateToken(account);
            return token;
        }
    }
}
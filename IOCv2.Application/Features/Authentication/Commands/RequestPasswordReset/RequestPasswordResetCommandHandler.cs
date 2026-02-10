using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text;

namespace IOCv2.Application.Features.Authentication.Commands.RequestPasswordReset
{
    internal class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly IRateLimiter _rateLimiter;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMessageService _messageService;

        public RequestPasswordResetCommandHandler(IUnitOfWork unitOfWork, IBackgroundEmailSender emailSender,
            IRateLimiter rateLimiter, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _rateLimiter = rateLimiter;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _messageService = messageService;
        }

        public async Task<Result<string>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
        {
            // Get IP address for rate limiting
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";
            var rateLimitKey = $"pr:{ipAddress}:{request.Email}";

            // Check rate limiting (3 requests per 10 minutes)
            if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
            {
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Auth.ResetRequestLimit));
            }

            // Find user by Email
            var user = await _unitOfWork.Repository<User>()
                    .Query()
                    .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            // Security: Always return the same message to avoid revealing if user exists
            var genericMessage = _messageService.GetMessage(MessageKeys.Auth.PasswordResetGenericMessage);

            // Register this attempt for rate limiting
            await _rateLimiter.RegisterFailAsync(
                rateLimitKey,
                limit: 3,
                window: TimeSpan.FromMinutes(10),
                blockFor: TimeSpan.FromMinutes(10),
                cancellationToken);

            if (user == null || user.Status != Domain.Enums.UserStatus.Active)
            {
                return Result<string>.Success(genericMessage);
            }

            // Generate random token (32 bytes = 256 bits)
            var tokenBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(tokenBytes);
            }
            var plainToken = Convert.ToHexString(tokenBytes).ToLower();

            // Hash the token before storing (SHA256)
            var tokenHash = ComputeSha256Hash(plainToken);

            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);

            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = expiresAt,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Repository<PasswordResetToken>().AddAsync(resetToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Generate reset link
            var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendUrl}/reset-password?token={plainToken}";

            // Send email
            await _emailSender.EnqueuePasswordResetEmailAsync(
                user.Email,
                resetLink,
                user.FullName,
                user.Id,
                null, // No PerformedBy info for seft-service
                cancellationToken);

            return Result<string>.Success(genericMessage);

        }

        private static string ComputeSha256Hash(string input)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = sha256.ComputeHash(bytes);
            return Convert.ToHexString(hashBytes).ToLower();
        }
    }
}

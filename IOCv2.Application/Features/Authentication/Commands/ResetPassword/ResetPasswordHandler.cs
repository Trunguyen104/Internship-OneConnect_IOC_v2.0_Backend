using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace IOCv2.Application.Features.Authentication.Commands.ResetPassword
{
    public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPasswordService _passwordService;
        private readonly IMessageService _messageService;
        public ResetPasswordHandler(IUnitOfWork unitOfWork, IMessageService messageService, IPasswordService passwordService)
        {
            _unitOfWork = unitOfWork;
            _passwordService = passwordService;
            _messageService = messageService;
        }
        public async Task<Result<string>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
        {
            var tokenHash = ComputeSha256Hash(request.Token);
            // Find the token in database by its unique hash
            var resetToken = await _unitOfWork.Repository<PasswordResetToken>()
                .Query()
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

            // Validate token exists and meets requirements
            // Generic message for all failure cases to prevent information disclosure
            var invalidLinkMessage = _messageService.GetMessage(MessageKeys.Auth.InvalidResetLink);

            if (resetToken == null)
            {
                return Result<string>.Failure(invalidLinkMessage);
            }

            // Validate token not expired
            if (resetToken.ExpiresAt < DateTimeOffset.UtcNow)
            {
                return Result<string>.Failure(invalidLinkMessage);
            }

            // Validate token not already used (one-time use)
            if (resetToken.IsUsed)
            {
                return Result<string>.Failure(invalidLinkMessage);
            }

            // Verify the owner of the token is active
            if (resetToken.User == null || resetToken.User.Status != IOCv2.Domain.Enums.UserStatus.Active)
            {
                return Result<string>.Failure(invalidLinkMessage);
            }

            // All validations passed - proceed with password reset
            var user = resetToken.User;

            // Hash and update the new password using rich domain method
            user.UpdatePassword(_passwordService.HashPassword(request.NewPassword));

            // Mark this token as used
            resetToken.UsedAt = DateTimeOffset.UtcNow;

            // Invalidate all other active tokens for this user (security measure)
            var otherActiveTokens = await _unitOfWork.Repository<PasswordResetToken>()
                .Query()
                .Where(t => t.UserId == user.UserId
                         && t.TokenId != resetToken.TokenId
                         && t.UsedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in otherActiveTokens)
            {
                token.UsedAt = DateTimeOffset.UtcNow;
            }

            // Log the password reset using the standardized AuditLog
            await _unitOfWork.Repository<AuditLog>().AddAsync(new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                Action = AuditAction.ResetPassword,
                EntityType = nameof(User),
                EntityId = user.UserId,
                PerformedById = user.UserId, // User reset their own password
                Reason = "ForgotPassword flow",
                CreatedAt = DateTime.UtcNow,
                Metadata = "{\"info\": \"Password reset via email token\"}"
            });

            // Save all changes
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<string>.Success(_messageService.GetMessage(MessageKeys.Auth.PasswordResetSuccess));
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

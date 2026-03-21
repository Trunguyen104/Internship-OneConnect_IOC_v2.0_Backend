using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Authentication.Commands.RevokeToken
{
    public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<RevokeTokenHandler> _logger;

        public RevokeTokenHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<RevokeTokenHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Revoking refresh token");

            var token = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

            if (token == null)
            {
                _logger.LogWarning("Refresh token not found for revoke request");
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidToken));
            }

            await _unitOfWork.Repository<Domain.Entities.RefreshToken>().DeleteAsync(token);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation("Refresh token revoked for user {UserId}", token.UserId);

            return Result<bool>.Success(true);
        }
    }
}

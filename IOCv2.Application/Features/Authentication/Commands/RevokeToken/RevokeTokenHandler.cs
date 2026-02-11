using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Authentication.Commands.RevokeToken
{
    public class RevokeTokenHandler : IRequestHandler<RevokeTokenCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;

        public RevokeTokenHandler(IUnitOfWork unitOfWork, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
        }

        public async Task<Result<bool>> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
        {
            var token = await _unitOfWork.Repository<Domain.Entities.RefreshToken>()
                .Query()
                .FirstOrDefaultAsync(x => x.Token == request.RefreshToken, cancellationToken);

            if (token == null)
            {
                return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.Auth.InvalidToken));
            }

            await _unitOfWork.Repository<Domain.Entities.RefreshToken>().DeleteAsync(token);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
    }
}

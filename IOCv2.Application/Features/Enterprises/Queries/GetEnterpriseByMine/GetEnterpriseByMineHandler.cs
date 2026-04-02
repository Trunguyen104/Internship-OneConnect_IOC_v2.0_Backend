using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseByMine
{
    public class GetEnterpriseByMineHandler : IRequestHandler<GetEnterpriseByMineCommand, Result<GetEnterpriseByMineResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetEnterpriseByMineHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRateLimiter _rateLimiter;

        public GetEnterpriseByMineHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService,
            ILogger<GetEnterpriseByMineHandler> logger, ICurrentUserService currentUserService, IRateLimiter rateLimiter)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
            _rateLimiter = rateLimiter;
        }

        public async Task<Result<GetEnterpriseByMineResponse>> Handle(GetEnterpriseByMineCommand request, CancellationToken cancellationToken)
        {

            // Each user has own key counting invalid turn
            var rateLimitKey = _messageService.GetMessage(MessageKeys.Enterprise.RateLimitGetByHRAttempt, _currentUserService.UserId ?? string.Empty);
            // Check if user is blocked due to too many failed attempts
            if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
            {
                return Result<GetEnterpriseByMineResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.RequestManyTimes), ResultErrorType.BadRequest);
            }
            // Register failed attempt (block after 30 attempts in 1 mins)
            await _rateLimiter.RegisterFailAsync(
                rateLimitKey,
                limit: 30,
                window: TimeSpan.FromMinutes(1),
                blockFor: TimeSpan.FromMinutes(1),
                cancellationToken);
            var userId = Guid.Parse(_currentUserService.UserId!);
            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().Select(x => x).FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
            if (enterpriseUser == null) return Result<GetEnterpriseByMineResponse>.NotFound(_messageService.GetMessage(MessageKeys.Enterprise.HRNotAssociatedWithEnterprise));
            var enterprise = await _unitOfWork.Repository<Enterprise>().GetByIdAsync(enterpriseUser!.EnterpriseId, cancellationToken);
            if (enterprise == null) return Result<GetEnterpriseByMineResponse>.NotFound(_messageService.GetMessage(MessageKeys.Enterprise.EnterpriseNotFoundCurrentHR));
            var response = _mapper.Map<GetEnterpriseByMineResponse>(enterprise);
            await _unitOfWork.CommitTransactionAsync();
            return Result<GetEnterpriseByMineResponse>.Success(response);

        }
    }
}
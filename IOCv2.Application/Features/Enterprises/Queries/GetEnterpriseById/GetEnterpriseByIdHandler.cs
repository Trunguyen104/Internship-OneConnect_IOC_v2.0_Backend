using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Enterprises.Commands.UpdateEnterprise;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Resources;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseById
{
    public class GetEnterpriseByIdHandler : MediatR.IRequestHandler<GetEnterpriseByIdQuery, Common.Models.Result<GetEnterpriseByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetEnterpriseByIdHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IRateLimiter _rateLimiter;
        private readonly ICurrentUserService _currentUserService;

        public GetEnterpriseByIdHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<GetEnterpriseByIdHandler> logger, IMapper mapper, IRateLimiter rateLimiter, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
            _rateLimiter = rateLimiter;
            _currentUserService = currentUserService;
        }
        public async Task<Result<GetEnterpriseByIdResponse>> Handle(GetEnterpriseByIdQuery request, CancellationToken cancellationToken)
        {
                var userId = Guid.Parse(_currentUserService.UserId!);
                // Each user has own key counting invalid turn
                var rateLimitKey = _messageService.GetMessage(MessageKeys.Enterprise.RateLimitGetByIDAttempt, _currentUserService.UserId!);
                // Check if user is blocked due to too many failed attempts
                if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
                {
                    return Result<GetEnterpriseByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.RequestManyTimes), ResultErrorType.TooManyRequests);
                }
                // Register failed attempt (block after 30 attempts in 1 mins)
                await _rateLimiter.RegisterFailAsync(
                    rateLimitKey,
                    limit: 30,
                    window: TimeSpan.FromMinutes(1),
                    blockFor: TimeSpan.FromMinutes(1),
                    cancellationToken);
                // Log the incoming request
                var enterprise = await _unitOfWork.Repository<Enterprise>().Query().AsNoTracking().FirstOrDefaultAsync(e => e.EnterpriseId == request.Id);

                // Check if the enterprise was found
                if (enterprise == null)
                {
                    _logger.LogError(_messageService.GetMessage(MessageKeys.Enterprise.NotFound));
                    return Result<GetEnterpriseByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Enterprise.NotFound),
                    ResultErrorType.NotFound);
                }
                if (!_currentUserService.Role!.Equals(UserRole.SuperAdmin.ToString()))
                {
                    // Verify that user belong to the target enterprise
                    if (!(await _unitOfWork.Repository<EnterpriseUser>().ExistsAsync(e => e.UserId == userId && enterprise.EnterpriseId == e.EnterpriseId)))
                        return Result<GetEnterpriseByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.GetByIDPermissionsNotAllowed), ResultErrorType.Forbidden);
                }
                var response = _mapper.Map<GetEnterpriseByIdResponse>(enterprise);
                return Result<GetEnterpriseByIdResponse>.Success(response);
        }
    }
}
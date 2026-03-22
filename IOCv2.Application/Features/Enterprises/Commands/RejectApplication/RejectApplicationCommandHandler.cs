using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Enterprises.Commands.RejectApplication;

public class RejectApplicationCommandHandler : IRequestHandler<RejectApplicationCommand, Result<RejectApplicationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IMapper _mapper;
    private readonly ILogger<RejectApplicationCommandHandler> _logger;

    public RejectApplicationCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IMapper mapper,
        ILogger<RejectApplicationCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<RejectApplicationResponse>> Handle(RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipApplication.LogRejecting), request.ApplicationId);

        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipApplication.LogInvalidUserId));
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<IOCv2.Domain.Entities.InternshipApplication>().Query()
                .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId &&
                                          a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.NotFound),
                    ResultErrorType.NotFound);

            if (app.Status != InternshipApplicationStatus.Pending)
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.StatusMustBePendingToReject),
                    ResultErrorType.BadRequest);

            app.Status = InternshipApplicationStatus.Rejected;
            app.RejectReason = request.RejectReason;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewedBy = enterpriseUser.EnterpriseUserId;

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipApplication.LogRejectSuccess), request.ApplicationId);

            var response = _mapper.Map<RejectApplicationResponse>(app);
            response.Message = _messageService.GetMessage(MessageKeys.InternshipApplication.RejectSuccess);

            return Result<RejectApplicationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipApplication.LogRejectError), request.ApplicationId);
            return Result<RejectApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }
    }
}

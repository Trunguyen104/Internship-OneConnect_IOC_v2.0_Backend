using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Enterprises.Commands.AssignMentor;

public class AssignMentorCommandHandler : IRequestHandler<AssignMentorCommand, Result<AssignMentorResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IMapper _mapper;
    private readonly ILogger<AssignMentorCommandHandler> _logger;

    public AssignMentorCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IMapper mapper,
        ILogger<AssignMentorCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AssignMentorResponse>> Handle(AssignMentorCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipApplication.LogAssigningMentor), request.ApplicationId);

        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipApplication.LogInvalidUserId));
                return Result<AssignMentorResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<AssignMentorResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<IOCv2.Domain.Entities.InternshipApplication>().Query()
                .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId &&
                                          a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<AssignMentorResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.NotFound),
                    ResultErrorType.NotFound);

            if (app.Status != InternshipApplicationStatus.Approved)
                return Result<AssignMentorResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.StatusMustBeApprovedToAssign),
                    ResultErrorType.BadRequest);

            var mentorUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.EnterpriseUserId == request.MentorEnterpriseUserId &&
                                           eu.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (mentorUser == null)
                return Result<AssignMentorResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.MentorNotBelongToEnterprise),
                    ResultErrorType.BadRequest);

            var alreadyInGroup = await _unitOfWork.Repository<InternshipStudent>().Query().AsNoTracking()
                .AnyAsync(ms => ms.StudentId == app.StudentId &&
                                ms.InternshipGroup.EnterpriseId == enterpriseUser.EnterpriseId &&
                                ms.InternshipGroup.TermId == app.TermId, cancellationToken);

            if (alreadyInGroup)
                return Result<AssignMentorResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.StudentAlreadyInGroup),
                    ResultErrorType.Conflict);

            var existingGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(ig => ig.Members)
                .FirstOrDefaultAsync(ig =>
                    ig.MentorId == request.MentorEnterpriseUserId &&
                    ig.EnterpriseId == enterpriseUser.EnterpriseId &&
                    ig.TermId == app.TermId, cancellationToken);

            InternshipGroup targetGroup;
            bool isNewGroup = existingGroup == null;

            if (!isNewGroup)
            {
                targetGroup = existingGroup!;
            }
            else
            {
                targetGroup = InternshipGroup.Create(
                    app.TermId,
                    $"Nhóm của Mentor - {DateTime.UtcNow:yyyyMMdd}",
                    enterpriseUser.EnterpriseId,
                    request.MentorEnterpriseUserId,
                    DateTime.UtcNow,
                    DateTime.UtcNow.AddMonths(4));

                await _unitOfWork.Repository<InternshipGroup>().AddAsync(targetGroup, cancellationToken);
            }

            targetGroup.AddMember(app.StudentId, InternshipRole.Member);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var successKey = isNewGroup
                ? MessageKeys.InternshipApplication.AssignMentorNewGroupSuccess
                : MessageKeys.InternshipApplication.AssignMentorExistingGroupSuccess;

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipApplication.LogAssignMentorSuccess), request.ApplicationId);

            var response = _mapper.Map<AssignMentorResponse>(targetGroup);
            response.ApplicationId = request.ApplicationId;
            response.Message = _messageService.GetMessage(successKey);

            return Result<AssignMentorResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipApplication.LogAssignMentorError), request.ApplicationId);
            return Result<AssignMentorResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }
    }
}

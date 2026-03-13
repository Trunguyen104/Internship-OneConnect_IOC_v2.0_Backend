using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Enterprises.Commands.AssignProject;

public class AssignProjectCommandHandler : IRequestHandler<AssignProjectCommand, Result<AssignProjectResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IMapper _mapper;
    private readonly ILogger<AssignProjectCommandHandler> _logger;

    public AssignProjectCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IMapper mapper,
        ILogger<AssignProjectCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<AssignProjectResponse>> Handle(AssignProjectCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipApplication.LogAssigningProject), request.ApplicationId);

        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipApplication.LogInvalidUserId));
                return Result<AssignProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var mentorEnterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (mentorEnterpriseUser == null)
                return Result<AssignProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<IOCv2.Domain.Entities.InternshipApplication>().Query().AsNoTracking()
                .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId &&
                                          a.EnterpriseId == mentorEnterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<AssignProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.NotFound),
                    ResultErrorType.NotFound);

            var membership = await _unitOfWork.Repository<InternshipStudent>().Query()
                .Include(ms => ms.InternshipGroup).ThenInclude(ig => ig.Projects)
                .FirstOrDefaultAsync(ms =>
                    ms.StudentId == app.StudentId &&
                    ms.InternshipGroup.MentorId == mentorEnterpriseUser.EnterpriseUserId &&
                    ms.InternshipGroup.EnterpriseId == mentorEnterpriseUser.EnterpriseId &&
                    ms.InternshipGroup.TermId == app.TermId, cancellationToken);

            if (membership == null)
                return Result<AssignProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.StudentNotInMentorGroup),
                    ResultErrorType.BadRequest);

            var group = membership.InternshipGroup;

            if (group.Projects.Any(p => p.ProjectName == request.ProjectName))
                return Result<AssignProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipApplication.ProjectNameExistsInGroup),
                    ResultErrorType.Conflict);

            var project = Project.Create(
                group.InternshipId,
                request.ProjectName,
                request.ProjectDescription);

            await _unitOfWork.Repository<Project>().AddAsync(project, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipApplication.LogAssignProjectSuccess), request.ApplicationId, request.ProjectName);

            var response = _mapper.Map<AssignProjectResponse>(project);
            response.InternshipGroupId = group.InternshipId;
            response.Message = _messageService.GetMessage(MessageKeys.InternshipApplication.AssignProjectSuccess);

            return Result<AssignProjectResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipApplication.LogAssignProjectError), request.ApplicationId);
            return Result<AssignProjectResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }
    }
}

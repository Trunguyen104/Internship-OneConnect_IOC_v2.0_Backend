using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, Result<UpdateProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateProjectHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUser;
        private readonly ICacheService _cacheService;

        public UpdateProjectHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UpdateProjectHandler> logger,
            IMessageService messageService, ICurrentUserService currentUser, ICacheService cacheService)
        {
            _unitOfWork    = unitOfWork;
            _mapper        = mapper;
            _logger        = logger;
            _messageService = messageService;
            _currentUser   = currentUser;
            _cacheService  = cacheService;
        }

        public async Task<Result<UpdateProjectResponse>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogUpdating), request.ProjectId, _currentUser.UserId);

            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .Include(p => p.InternshipGroup)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound), ResultErrorType.NotFound);
            }

            // Scope check
            if (project.MentorId != enterpriseUser.EnterpriseUserId &&
                project.InternshipGroup?.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            // Block nếu status = Completed hoặc Archived
            if (project.Status == ProjectStatus.Completed || project.Status == ProjectStatus.Archived)
                return Result<UpdateProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.InvalidStatusForUpdate), ResultErrorType.BadRequest);

            // Block nếu group Archived/Finished
            var groupStatus = project.InternshipGroup?.Status;
            if (groupStatus == GroupStatus.Archived || groupStatus == GroupStatus.Finished)
                return Result<UpdateProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.GroupNotActiveForUpdate), ResultErrorType.BadRequest);

            if (request.InternshipId.HasValue && request.InternshipId.Value == Guid.Empty)
                return Result<UpdateProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InvalidRequest), ResultErrorType.BadRequest);

            var assignedCount = 0;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Check target internship nếu thay đổi
                if (request.InternshipId.HasValue && request.InternshipId != Guid.Empty && request.InternshipId != project.InternshipId)
                {
                    var targetGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.InternshipId == request.InternshipId.Value, cancellationToken);

                    if (targetGroup == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Internships.NotFound), ResultErrorType.NotFound);
                    }

                    if (targetGroup.Status == GroupStatus.Archived || targetGroup.Status == GroupStatus.Finished)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Projects.GroupNotActiveForUpdate), ResultErrorType.BadRequest);
                    }

                    if (targetGroup.MentorId != enterpriseUser.EnterpriseUserId)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                    }
                }

                // Uniqueness check: ProjectName nếu thay đổi
                if (request.ProjectName is not null && project.ProjectName != request.ProjectName)
                {
                    var nameExists = await _unitOfWork.Repository<Project>()
                        .ExistsAsync(p => p.InternshipId == (request.InternshipId ?? project.InternshipId)
                                       && p.ProjectName == request.ProjectName
                                       && p.ProjectId != request.ProjectId, cancellationToken);
                    if (nameExists)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Projects.ProjectNameExistsInternship), ResultErrorType.Conflict);
                    }
                }

                project.Update(
                    request.InternshipId,
                    request.ProjectName,
                    request.Description,
                    request.StartDate,
                    request.EndDate,
                    null, // Status không được phép thay đổi qua UpdateProject
                    request.Field,
                    request.Requirements,
                    request.Deliverables,
                    request.Template);

                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Projects.LogUpdateError), request.ProjectId);
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }

            await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogUpdateSuccess), request.ProjectId);

            // Đếm sinh viên trong group (thay thế ProjectAssignment cũ)
            if (project.InternshipId.HasValue)
            {
                assignedCount = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .CountAsync(s => s.InternshipId == project.InternshipId.Value, cancellationToken);
            }

            var response = _mapper.Map<UpdateProjectResponse>(project);
            response.AssignedStudentCount = assignedCount;

            return Result<UpdateProjectResponse>.Success(response);
        }
    }
}

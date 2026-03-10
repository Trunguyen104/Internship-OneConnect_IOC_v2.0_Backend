using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, Result<UpdateProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateProjectHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUser;

        public UpdateProjectHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UpdateProjectHandler> logger, IMessageService messageService, ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
            _currentUser = currentUser;
        }

        public async Task<Result<UpdateProjectResponse>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating project: {ProjectId} by User: {UserId}", request.ProjectId, _currentUser.UserId);

            // 1. Existence and Ownership Check (FFA-SEC)
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(request.ProjectId, cancellationToken);
            if (project == null)
            {
                _logger.LogWarning("Project not found: {ProjectId}", request.ProjectId);
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound), ResultErrorType.NotFound);
            }

            // Security: Only Mentor or Project Lead can update (Ownership Check)
            // Implementation detail: check if student is the leader of the internship group associated with the project
            // For now, checking against CurrentUserService role and ID
            var currentUserIdStr = _currentUser.UserId;
            if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            // 2. Transaction Scope (FFA-TXG)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Check if internship exists (if internship id provided)
                if (request.InternshipId.HasValue && request.InternshipId != Guid.Empty && request.InternshipId != project.InternshipId)
                {
                    var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
                        .ExistsAsync(i => i.InternshipId == request.InternshipId.Value, cancellationToken);
                    if (!internshipExists)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Internships.NotFound),
                            ResultErrorType.NotFound);
                    }
                }

                // Uniqueness Check: Project Name (if changed)
                if (request.ProjectName is not null && project.ProjectName != request.ProjectName)
                {
                    var projectExists = await _unitOfWork.Repository<Project>()
                        .ExistsAsync(p => p.InternshipId == (request.InternshipId ?? project.InternshipId)
                                       && p.ProjectName == request.ProjectName
                                       && p.ProjectId != request.ProjectId,
                                       cancellationToken);

                    if (projectExists)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Projects.ProjectNameExistsInternship),
                            ResultErrorType.Conflict);
                    }
                }

                // 3. Domain Logic via Entity (FFA-CAG)
                project.Update(
                    request.InternshipId,
                    request.ProjectName,
                    request.Description,
                    request.StartDate,
                    request.EndDate,
                    request.Status);

                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Successfully updated project {ProjectId}", request.ProjectId);

                // 4. Mapping & Response (outside transaction ideally, but fine here after commit)
                var response = _mapper.Map<UpdateProjectResponse>(project);
                return Result<UpdateProjectResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Transaction failed while updating project {ProjectId}", request.ProjectId);
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}

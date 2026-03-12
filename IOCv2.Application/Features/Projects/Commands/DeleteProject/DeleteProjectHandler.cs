using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Services;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.DeleteProject
{
    public class DeleteProjectHandler : IRequestHandler<DeleteProjectCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteProjectHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUser;

        public DeleteProjectHandler(IUnitOfWork unitOfWork, ILogger<DeleteProjectHandler> logger, IMessageService messageService, ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
            _currentUser = currentUser;
        }

        public async Task<Result<string>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Attempting to delete project: {ProjectId} by User: {UserId}", request.ProjectId, _currentUser.UserId);

            // 1. Existence and Ownership Check (FFA-SEC)
            var project = await _unitOfWork.Repository<Project>().GetByIdAsync(request.ProjectId, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning("Project not found for deletion: {ProjectId}", request.ProjectId);
                return Result<string>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.NotFound),
                    ResultErrorType.NotFound);
            }

            // Security: Ownership check (Implementation same as update)
            var currentUserIdStr = _currentUser.UserId;
            if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            // 2. Logic & Persistence (FFA-FLW)
            // Soft delete
            project.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted project {ProjectId}", request.ProjectId);

                return Result<string>.Success(_messageService.GetMessage(MessageKeys.Projects.DeleteSuccess));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Transaction failed while deleting project {ProjectId}", request.ProjectId);
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}

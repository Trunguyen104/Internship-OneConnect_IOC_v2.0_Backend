using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource
{
    /// <summary>
    /// Handler responsible for updating an existing ProjectResource.
    /// Uses the unit of work / repository pattern to ensure update occurs within a transaction.
    /// </summary>
    public class UpdateProjectResourceHandler : IRequestHandler<UpdateProjectResourceCommand, Result<UpdateProjectResourceResponse>>
    {
        private readonly ILogger<UpdateProjectResourceHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        /// <summary>
        /// Construct the handler with required dependencies.
        /// </summary>
        public UpdateProjectResourceHandler(ILogger<UpdateProjectResourceHandler> logger, IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        /// <summary>
        /// Handles the update request:
        /// 1. Ensure the resource exists.
        /// 2. Ensure the target project exists.
        /// 3. Begin a transaction, update entity, persist and commit.
        /// 4. Roll back and log on errors.
        /// </summary>
        public async Task<Result<UpdateProjectResourceResponse>> Handle(UpdateProjectResourceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Verify the resource exists before attempting update.
                var projectResource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().GetByIdAsync(request.ProjectResourceId);
                if (projectResource == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ProjectResourceId);
                    return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound), ResultErrorType.NotFound);
                }

                // Verify the provided ProjectId refers to an existing project.
                var projectExists = await _unitOfWork.Repository<Domain.Entities.Project>().ExistsAsync(p => p.ProjectId == request.ProjectId, cancellationToken);
                if (!projectExists)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                    return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound), ResultErrorType.NotFound);
                }

                // Begin transaction to ensure atomic update.
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Update domain entity state using its domain method.
                projectResource.UpdateInfo(request.ProjectId, request.ResourceName, request.ResourceType);

                // Persist changes through repository + unit of work.
                await _unitOfWork.Repository<Domain.Entities.ProjectResources>().UpdateAsync(projectResource, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Commit transaction after successful save.
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Map to response DTO and return success.
                var response = _mapper.Map<UpdateProjectResourceResponse>(projectResource);
                return Result<UpdateProjectResourceResponse>.Success(response);
            }
            catch (Exception ex)
            {
                // Rollback any started transaction and return a conflict result.
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUpdateError), request.ProjectResourceId);
                return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.UpdateError), ResultErrorType.Conflict);
            }
        }
    }
}

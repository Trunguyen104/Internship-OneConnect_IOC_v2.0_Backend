using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.DeleteProjectResource
{
    /// <summary>
    /// Handles deletion of a ProjectResource. Uses the unit of work + repository pattern
    /// to perform a soft delete inside a transaction and returns a Result wrapper.
    /// </summary>
    public class DeleteProjectResourceHandler : IRequestHandler<DeleteProjectResourceCommand, Result<DeleteProjectResourceResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteProjectResourceHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        /// <summary>
        /// Constructs the handler with required dependencies.
        /// </summary>
        public DeleteProjectResourceHandler(IUnitOfWork unitOfWork, ILogger<DeleteProjectResourceHandler> logger
            , IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _messageService = messageService;
        }

        /// <summary>
        /// Handles the DeleteProjectResourceCommand.
        /// Steps:
        /// 1. Verify the resource exists.
        /// 2. Begin a transaction.
        /// 3. Perform a repository-level delete (configured as a soft delete).
        /// 4. Persist changes and commit the transaction.
        /// 5. Map the deleted entity to a response DTO and return success.
        /// On any exception, roll back the transaction, log the error and return a failure result.
        /// </summary>
        public async Task<Result<DeleteProjectResourceResponse>> Handle(DeleteProjectResourceCommand request, CancellationToken cancellationToken)
        {
            // Ensure the resource exists before attempting deletion.
            var resource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().GetByIdAsync(request.ResourceId);
            if (resource == null)
            {
                // Resource not found — log a warning and return NotFound result.
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ResourceId);
                return Result<DeleteProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound), ResultErrorType.NotFound);
            }

            try
            {
                // Begin transaction scope so delete + save are atomic.
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Repository.DeleteAsync is expected to perform a soft delete according to repository configuration.
                await _unitOfWork.Repository<Domain.Entities.ProjectResources>().DeleteAsync(resource, cancellationToken);

                // Persist the change to the data store.
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Commit transaction after successful save.
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Successful deletion — log and return mapped response.
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogDeleteSuccess), request.ResourceId);
                var response = _mapper.Map<DeleteProjectResourceResponse>(resource);
                return Result<DeleteProjectResourceResponse>.Success(response);
            }
            catch (Exception ex)
            {
                // Ensure any started transaction is rolled back on error.
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                // Log the exception with context and return a generic conflict/internal error response.
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogDeleteError), request.ResourceId);
                return Result<DeleteProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}

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
    public class DeleteProjectResourceHandler : IRequestHandler<DeleteProjectResourceCommand, Result<DeleteProjectResourceResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteProjectResourceHandler> _logger;
        private readonly IMapper _mapper; private readonly IMessageService _messageService;
        public DeleteProjectResourceHandler(IUnitOfWork unitOfWork, ILogger<DeleteProjectResourceHandler> logger
            , IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<DeleteProjectResourceResponse>> Handle(DeleteProjectResourceCommand request, CancellationToken cancellationToken)
        {
            // Check if the resource exists
            var resource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().GetByIdAsync(request.ResourceId);
            if (resource == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ResourceId);
                return Result<DeleteProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound));
            }
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Soft delete the resource properly via the Repository configuration
                await _unitOfWork.Repository<Domain.Entities.ProjectResources>().DeleteAsync(resource, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogDeleteSuccess), request.ResourceId);
                var response = _mapper.Map<DeleteProjectResourceResponse>(resource);
                return Result<DeleteProjectResourceResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to delete project resource: {ResourceId}", request.ResourceId);
                throw;
            }
        }
    }
}

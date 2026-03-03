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

namespace IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource
{
    public class UpdateProjectResourceHandler : IRequestHandler<UpdateProjectResourceCommand, Result<UpdateProjectResourceResponse>>
    {
        private readonly ILogger<UpdateProjectResourceHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public UpdateProjectResourceHandler(ILogger<UpdateProjectResourceHandler> logger, IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }
        public async Task<Result<UpdateProjectResourceResponse>> Handle(UpdateProjectResourceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if the project resource exists
                var projectResource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().GetByIdAsync(request.ProjectResourceId);
                if (projectResource == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ProjectResourceId);
                    return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound));
                }
                var projectExists = await _unitOfWork.Repository<Domain.Entities.Project>().ExistsAsync(p => p.ProjectId == request.ProjectId, cancellationToken);
                if (!projectExists)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                    return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound));
                }
                // Update the project resource properties
                projectResource.ProjectId = request.ProjectId;
                projectResource.ResourceName = request.ResourceName;
                projectResource.ResourceType = request.ResourceType;
                await _unitOfWork.Repository<Domain.Entities.ProjectResources>().UpdateAsync(projectResource);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                var response = _mapper.Map<UpdateProjectResourceResponse>(projectResource);
                return Result<UpdateProjectResourceResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUpdateError), request.ProjectResourceId);
                return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.UpdateError));
            }
        }
    }
}

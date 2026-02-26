using AutoMapper;
using IOCv2.Application.Common.Models;
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
        private readonly IMapper _mapper;
        public DeleteProjectResourceHandler(IUnitOfWork unitOfWork, ILogger<DeleteProjectResourceHandler> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<Result<DeleteProjectResourceResponse>> Handle(DeleteProjectResourceCommand request, CancellationToken cancellationToken)
        {
            var resource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().GetByIdAsync(request.ResourceId);
            if (resource == null)
            {
                _logger.LogWarning("Project resource with ID {ResourceId} not found.", request.ResourceId);
                return Result<DeleteProjectResourceResponse>.Failure("Project resource not found.");
            }
            resource.DeletedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<Domain.Entities.ProjectResources>().UpdateAsync(resource, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            _logger.LogInformation("Project resource with ID {ResourceId} deleted successfully.", request.ResourceId);
            var response = _mapper.Map<DeleteProjectResourceResponse>(resource);
            return Result<DeleteProjectResourceResponse>.Success(response);
        }
    }
}

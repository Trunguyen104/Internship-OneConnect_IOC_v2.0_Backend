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

        public UpdateProjectHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UpdateProjectHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }
        public async Task<Result<UpdateProjectResponse>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get project by id
                var project = await _unitOfWork.Repository<Project>().GetByIdAsync(request.ProjectId, cancellationToken);
                // Check if project exists
                if (project == null)
                {
                    return Result<UpdateProjectResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Projects.NotFound),
                        ResultErrorType.NotFound);
                }
                // Check if new project name already exists for this internship (if name changed)
                if (project.ProjectName != request.ProjectName)
                {
                    var projectExists = await _unitOfWork.Repository<Project>()
                        .ExistsAsync(p => p.InternshipId == project.InternshipId
                                       && p.ProjectName == request.ProjectName
                                       && p.ProjectId != request.ProjectId,
                                       cancellationToken);

                    if (projectExists)
                    {
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Projects.ProjectNameExistsInternship),
                            ResultErrorType.Conflict);
                    }
                }
                // Map command to entity
                _mapper.Map(request, project);
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                // Map to response
                var response = _mapper.Map<UpdateProjectResponse>(project);

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.UpdateSuccess), request.ProjectId);

                return Result<UpdateProjectResponse>.Success(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Projects.UpdateError), request.ProjectId);
                throw;
            }
        }
    }
}

using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.CreateProject
{
    public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, Result<CreateProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateProjectHandler> _logger;
        private readonly IMessageService _message;
        public CreateProjectHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CreateProjectHandler> logger, ICurrentUserService currentUserService, IMessageService message)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _message = message;
        }
        public async Task<Result<CreateProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if the internship exists
                var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
                    .ExistsAsync(i => i.InternshipId == request.InternshipId, cancellationToken);

                if (!internshipExists)
                {
                    return Result<CreateProjectResponse>.Failure(
                        _message.GetMessage(MessageKeys.Internships.NotFound),
                        ResultErrorType.NotFound);
                }

                // Check if the project name exists within the internship
                var projectExists = await _unitOfWork.Repository<Project>()
                    .ExistsAsync(p => p.InternshipId == request.InternshipId
                                   && p.ProjectName == request.ProjectName, cancellationToken);
                if (projectExists)
                {
                    return Result<CreateProjectResponse>.Failure(
                        _message.GetMessage(MessageKeys.Projects.ProjectNameExistsInternship),
                        ResultErrorType.Conflict);
                }

                // Create new project
                var project = new Project(request.InternshipId, request.ProjectName, request.Description);
                await _unitOfWork.Repository<Project>().AddAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                var response = _mapper.Map<CreateProjectResponse>(project);
                _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogCreateSuccess), project.ProjectId, request.InternshipId);

                return Result<CreateProjectResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogCreateError), request.InternshipId);
                throw;
            }
        }
    }
}

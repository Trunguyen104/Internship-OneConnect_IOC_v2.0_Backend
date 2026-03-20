using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
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
        private readonly ICacheService _cacheService;
        public CreateProjectHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CreateProjectHandler> logger, IMessageService message, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _message = message;
            _cacheService = cacheService;
        }
        public async Task<Result<CreateProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new project: {ProjectName} for Internship: {InternshipId}", request.ProjectName, request.InternshipId);

            // 1. Existence Check: Internship
            var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
                .ExistsAsync(i => i.InternshipId == request.InternshipId, cancellationToken);

            if (!internshipExists)
            {
                _logger.LogWarning("Internship not found: {InternshipId}", request.InternshipId);
                return Result<CreateProjectResponse>.Failure(
                    _message.GetMessage(MessageKeys.Internships.NotFound),
                    ResultErrorType.NotFound);
            }

            // 2. Uniqueness Check: Project Name
            var projectExists = await _unitOfWork.Repository<Project>()
                .ExistsAsync(p => p.InternshipId == request.InternshipId
                               && p.ProjectName == request.ProjectName, cancellationToken);
            if (projectExists)
            {
                _logger.LogWarning("Project name already exists in internship: {ProjectName}", request.ProjectName);
                return Result<CreateProjectResponse>.Failure(
                    _message.GetMessage(MessageKeys.Projects.ProjectNameExistsInternship),
                    ResultErrorType.Conflict);
            }

            // 3. Domain Logic & Persistence (FFA-CAG)
            var project = Project.Create(
                request.InternshipId, 
                request.ProjectName, 
                request.Description,
                request.StartDate,
                request.EndDate);
            
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().AddAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

                // 4. Mapping & Response (FFA-FLW)
                _logger.LogInformation("Successfully created project {ProjectId} for Internship {InternshipId}", project.ProjectId, request.InternshipId);
                
                var response = _mapper.Map<CreateProjectResponse>(project);
                return Result<CreateProjectResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogCreateError));
                return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}

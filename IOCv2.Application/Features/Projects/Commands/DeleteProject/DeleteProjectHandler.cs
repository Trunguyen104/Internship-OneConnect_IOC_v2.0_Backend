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
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteProjectHandler> _logger;
        private readonly IMessageService _messageService;
        public DeleteProjectHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<DeleteProjectHandler> logger, IMessageService messageService)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _messageService = messageService;
        }
        public async Task<Result<string>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get project by id
                var project = await _unitOfWork.Repository<Project>()
                    .GetByIdAsync(request.ProjectId, cancellationToken);

                // Check if project exists
                if (project == null)
                {
                    return Result<string>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.NotFound),
                    ResultErrorType.NotFound);
                }

                // Delete project (soft delete or hard delete?)
                project.DeletedAt = DateTime.UtcNow;
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);

                // Save changes
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogDelete), request.ProjectId);

                return Result<string>.Success(_messageService.GetMessage(MessageKeys.Projects.DeleteSuccess));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Projects.LogDeleteError), request.ProjectId);
                throw;
            }
        }
    }
}

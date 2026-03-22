using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Services;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetProjectRescourceById
{
    public class GetDownloadProjectResourceByIdHandler : IRequestHandler<GetDownloadProjectResourceByIdQuery, Result<GetDownloadProjectResourceByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDownloadProjectResourceByIdHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICurrentUserService _currentUserService;
        public GetDownloadProjectResourceByIdHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetDownloadProjectResourceByIdHandler> logger, IMessageService messageService, IFileStorageService fileStorageService, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _messageService = messageService;
            _fileStorageService = fileStorageService;
            _currentUserService = currentUserService;
        }
        public async Task<Result<GetDownloadProjectResourceByIdResponse>> Handle(GetDownloadProjectResourceByIdQuery request, CancellationToken cancellationToken)
        {

            // Check if the project resource exists
            var resource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().Query().AsNoTracking().FirstOrDefaultAsync(x => x.ProjectResourceId == request.ProjectResourceId, cancellationToken);
            if (resource == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ProjectResourceId);
                return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                    ResultErrorType.NotFound);
            }

            var hasAccess = await HasProjectAccessAsync(resource.ProjectId, cancellationToken);
            if (!hasAccess)
            {
                return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden),
                    ResultErrorType.Forbidden);
            }

            if (resource.ResourceType == FileType.LINK)
            {
                return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LinkDownloadNotSupported),
                    ResultErrorType.BadRequest);
            }

            // get stream from storage
            var stream = await _fileStorageService.GetFileAsync(resource.ResourceUrl, cancellationToken);
            if (stream == null)
            {
                _logger.LogWarning("File not found in storage for resource: {ResourceId}", request.ProjectResourceId);
                return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                    "File not found in storage.",
                    ResultErrorType.NotFound);
            }

            var extension = Path.GetExtension(resource.ResourceUrl);
            var safeResourceName = string.IsNullOrWhiteSpace(resource.ResourceName)
                ? request.ProjectResourceId.ToString("N")
                : resource.ResourceName;
            var response = new GetDownloadProjectResourceByIdResponse
            {
                Content = stream,
                ContentType = FileValidationHelper.GetMimeType(safeResourceName),
                FileName = $"{safeResourceName}{extension}"
            };

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdSuccess), request.ProjectResourceId);
            return Result<GetDownloadProjectResourceByIdResponse>.Success(response);

        }

        private async Task<bool> HasProjectAccessAsync(Guid projectId, CancellationToken cancellationToken)
        {
            if (string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUserService.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return false;
            }

            var studentId = await _unitOfWork.Repository<Domain.Entities.Student>().Query()
                .AsNoTracking()
                .Where(s => s.UserId == currentUserId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentId == Guid.Empty)
            {
                return false;
            }

            var internshipId = await _unitOfWork.Repository<Domain.Entities.Project>().Query()
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => p.InternshipId)
                .FirstOrDefaultAsync(cancellationToken);

            if (internshipId == Guid.Empty)
            {
                return false;
            }

            return await _unitOfWork.Repository<Domain.Entities.InternshipStudent>().Query()
                .AsNoTracking()
                .AnyAsync(m => m.InternshipId == internshipId && m.StudentId == studentId, cancellationToken);
        }
    }
}

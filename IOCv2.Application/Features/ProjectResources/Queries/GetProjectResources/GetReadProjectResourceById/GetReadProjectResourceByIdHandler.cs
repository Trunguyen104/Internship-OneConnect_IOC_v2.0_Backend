using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.ProjectResources.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetReadProjectResourceById
{
    public class GetReadProjectResourceByIdHandler : IRequestHandler<GetReadProjectResourceByIdQuery, Result<GetReadProjectResourceByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetReadProjectResourceByIdHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        public GetReadProjectResourceByIdHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetReadProjectResourceByIdHandler> logger, IMessageService messageService, IFileStorageService fileStorageService, ICurrentUserService currentUserService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _messageService = messageService;
            _fileStorageService = fileStorageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }
        public async Task<Result<GetReadProjectResourceByIdResponse>> Handle(GetReadProjectResourceByIdQuery request, CancellationToken cancellationToken)
        {

            var cacheKey = ProjectResourceCacheKeys.Read(request.ResourceId);
            var cached = await _cacheService.GetAsync<GetReadProjectResourceByIdResponse>(cacheKey, cancellationToken);
            if (cached != null)
            {
                var hasCachedAccess = await HasProjectAccessAsync(cached.ProjectId, cancellationToken);
                if (!hasCachedAccess)
                {
                    return Result<GetReadProjectResourceByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }

                return Result<GetReadProjectResourceByIdResponse>.Success(cached);
            }

            var resource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProjectResourceId == request.ResourceId, cancellationToken);
            if (resource == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ResourceId);
                return Result<GetReadProjectResourceByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                    ResultErrorType.NotFound);
            }

            var hasAccess = await HasProjectAccessAsync(resource.ProjectId, cancellationToken);
            if (!hasAccess)
            {
                return Result<GetReadProjectResourceByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden),
                    ResultErrorType.Forbidden);
            }

            if (resource.ResourceType != FileType.LINK)
            {
                resource.ResourceUrl = _fileStorageService.GetFileUrl(resource.ResourceUrl);
                if (!Uri.IsWellFormedUriString(resource.ResourceUrl, UriKind.Absolute))
                {
                    resource.ResourceUrl = _fileStorageService.GetDomainUrl() + resource.ResourceUrl;
                }
            }

            var response = _mapper.Map<GetReadProjectResourceByIdResponse>(resource);

            await _cacheService.SetAsync(cacheKey, response, ProjectResourceCacheKeys.Expiration.Read, cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdSuccess), request.ResourceId);
            return Result<GetReadProjectResourceByIdResponse>.Success(response);

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

            if (string.Equals(_currentUserService.Role, "Mentor", StringComparison.OrdinalIgnoreCase))
            {
                var enterpriseUserId = await _unitOfWork.Repository<Domain.Entities.EnterpriseUser>().Query()
                    .AsNoTracking()
                    .Where(eu => eu.UserId == currentUserId)
                    .Select(eu => eu.EnterpriseUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (enterpriseUserId == Guid.Empty)
                {
                    return false;
                }

                return await _unitOfWork.Repository<Domain.Entities.Project>().Query()
                    .AsNoTracking()
                    .AnyAsync(p => p.ProjectId == projectId && p.MentorId == enterpriseUserId, cancellationToken);
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

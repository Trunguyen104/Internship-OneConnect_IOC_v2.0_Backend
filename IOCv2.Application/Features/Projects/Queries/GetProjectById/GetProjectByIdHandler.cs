using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    public class GetProjectByIdHandler : IRequestHandler<GetProjectByIdQuery, Result<GetProjectByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetProjectByIdHandler> _logger;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService? _currentUserService;
        private readonly IFileStorageService? _fileStorageService;
        public GetProjectByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetProjectByIdHandler> logger,
            IMessageService message,
            ICacheService cacheService,
            ICurrentUserService? currentUserService = null,
            IFileStorageService? fileStorageService = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _message = message;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
        }
        public async Task<Result<GetProjectByIdResponse>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogGetById), request.ProjectId);

            // B5: Load project TRƯỚC khi kiểm tra cache để role check chạy trên dữ liệu thực
            var project = await _unitOfWork.Repository<Domain.Entities.Project>()
                .Query()
                .Include(x => x.ProjectResources)
                .Include(x => x.InternshipGroup!)
                    .ThenInclude(g => g.Mentor!)
                        .ThenInclude(m => m.User!)
                .Include(x => x.InternshipGroup!)
                    .ThenInclude(g => g.Members!)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning(_message.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                return Result<GetProjectByIdResponse>.Failure(
                    _message.GetMessage(MessageKeys.Projects.NotFound),
                    ResultErrorType.NotFound);
            }

            var role = _currentUserService?.Role ?? string.Empty;
            var isSuperAdmin = string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            var isMentor = string.Equals(role, "Mentor", StringComparison.OrdinalIgnoreCase);
            var isStudent = string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase);

            // B4: Mentor isolation — Mentor chỉ được xem project của chính mình
            if (isMentor)
            {
                if (!Guid.TryParse(_currentUserService?.UserId, out var mentorUserId))
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

                var mentorEnterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == mentorUserId, cancellationToken);

                if (mentorEnterpriseUser == null || project.MentorId != mentorEnterpriseUser.EnterpriseUserId)
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }
            // B3: Student check dùng two-layer model (không dùng legacy Status)
            else if (isStudent)
            {
                if (project.VisibilityStatus != VisibilityStatus.Published ||
                    (project.OperationalStatus != OperationalStatus.Active &&
                     project.OperationalStatus != OperationalStatus.Completed))
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                if (!project.InternshipId.HasValue)
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                if (!Guid.TryParse(_currentUserService?.UserId, out var currentUserId))
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

                var studentId = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .Where(s => s.Student.UserId == currentUserId)
                    .Select(s => s.StudentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (studentId == Guid.Empty)
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                var isMember = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .AnyAsync(s => s.InternshipId == project.InternshipId.Value && s.StudentId == studentId, cancellationToken);

                if (!isMember)
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }
            // B10: HR, UniAdmin, SchoolAdmin, EnterpriseAdmin — chỉ xem Published
            else if (!isSuperAdmin)
            {
                if (project.VisibilityStatus != VisibilityStatus.Published)
                    return Result<GetProjectByIdResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }

            // B5: Cache sau khi role check thành công
            var cacheKey = ProjectCacheKeys.Project(request.ProjectId);
            var cached = await _cacheService.GetAsync<GetProjectByIdResponse>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return Result<GetProjectByIdResponse>.Success(cached);
            }

            var response = _mapper.Map<GetProjectByIdResponse>(project);
            ResolveResourceUrls(response.ProjectResources);
            await _cacheService.SetAsync(cacheKey, response, ProjectCacheKeys.Expiration.Project, cancellationToken);
            return Result<GetProjectByIdResponse>.Success(response);
        }

        private void ResolveResourceUrls(List<ProjectResourcesDTO> resources)
        {
            if (_fileStorageService == null || resources.Count == 0)
            {
                return;
            }

            foreach (var resource in resources)
            {
                if (resource.ResourceType == FileType.LINK || string.IsNullOrWhiteSpace(resource.ResourceUrl))
                {
                    continue;
                }

                resource.ResourceUrl = _fileStorageService.GetFileUrl(resource.ResourceUrl);
                if (!Uri.IsWellFormedUriString(resource.ResourceUrl, UriKind.Absolute))
                {
                    resource.ResourceUrl = _fileStorageService.GetDomainUrl() + resource.ResourceUrl;
                }
            }
        }
    }
}

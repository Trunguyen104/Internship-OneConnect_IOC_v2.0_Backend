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
        public GetProjectByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetProjectByIdHandler> logger,
            IMessageService message,
            ICacheService cacheService,
            ICurrentUserService? currentUserService = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _message = message;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
        }
        public async Task<Result<GetProjectByIdResponse>> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogGetById), request.ProjectId);

            var cacheKey = ProjectCacheKeys.Project(request.ProjectId);
            var cached = await _cacheService.GetAsync<GetProjectByIdResponse>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return Result<GetProjectByIdResponse>.Success(cached);
            }

            var project = await _unitOfWork.Repository<Domain.Entities.Project>()
                .Query()
                .Include(x => x.ProjectResources)
                .Include(x => x.InternshipGroup)
                    .ThenInclude(g => g.Mentor)
                        .ThenInclude(m => m.User)
                .Include(x => x.InternshipGroup)
                    .ThenInclude(g => g.Members)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning(_message.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                return Result<GetProjectByIdResponse>.Failure(
                    _message.GetMessage(MessageKeys.Projects.NotFound),
                    ResultErrorType.NotFound);
            }

            var isStudent = string.Equals(_currentUserService?.Role, "Student", StringComparison.OrdinalIgnoreCase);
            if (isStudent)
            {
                if (project.Status != ProjectStatus.Published && project.Status != ProjectStatus.Completed)
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

            var response = _mapper.Map<GetProjectByIdResponse>(project);
            await _cacheService.SetAsync(cacheKey, response, ProjectCacheKeys.Expiration.Project, cancellationToken);
            return Result<GetProjectByIdResponse>.Success(response);
        }
    }
}

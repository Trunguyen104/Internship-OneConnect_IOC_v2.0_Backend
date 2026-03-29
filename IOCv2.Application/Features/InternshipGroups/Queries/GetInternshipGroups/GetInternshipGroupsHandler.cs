using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups
{
    public class GetInternshipGroupsHandler : IRequestHandler<GetInternshipGroupsQuery, Result<PaginatedResult<GetInternshipGroupsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetInternshipGroupsHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public GetInternshipGroupsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ICacheService cacheService,
            ILogger<GetInternshipGroupsHandler> logger,
            ICurrentUserService currentUserService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _cacheService = cacheService;
            _logger = logger;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<PaginatedResult<GetInternshipGroupsResponse>>> Handle(GetInternshipGroupsQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<PaginatedResult<GetInternshipGroupsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var role = _currentUserService.Role ?? string.Empty;

            // ── Phân quyền: xác định filter bổ sung theo role ─────────────────
            Guid? forcedEnterpriseId = null;
            Guid? forcedMentorId = null;
            Guid? forcedStudentId = null;

            if (string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "EnterpriseAdmin", StringComparison.OrdinalIgnoreCase))
            {
                // HR / EnterpriseAdmin: chỉ thấy nhóm của enterprise mình — bắt buộc, không cho override
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

                if (enterpriseUser == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound));
                    return Result<PaginatedResult<GetInternshipGroupsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                        ResultErrorType.Forbidden);
                }

                forcedEnterpriseId = enterpriseUser.EnterpriseId;
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogScopedHrEnterprise), currentUserId, forcedEnterpriseId);
            }
            else if (string.Equals(role, "Mentor", StringComparison.OrdinalIgnoreCase))
            {
                // Mentor: chỉ thấy nhóm mà mình là Mentor
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

                if (enterpriseUser == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound));
                    return Result<PaginatedResult<GetInternshipGroupsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                        ResultErrorType.Forbidden);
                }

                forcedMentorId = enterpriseUser.EnterpriseUserId;
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogScopedMentor), currentUserId, forcedMentorId);
            }
            else if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                // Student: chỉ thấy nhóm mà mình là thành viên
                var student = await _unitOfWork.Repository<Student>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

                if (student == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogStudentNotFoundForUser), currentUserId);
                    return Result<PaginatedResult<GetInternshipGroupsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.Users.NotFound),
                        ResultErrorType.NotFound);
                }

                forcedStudentId = student.StudentId;
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogScopedStudent), currentUserId, forcedStudentId);
            }
            // SuperAdmin / SchoolAdmin: không filter bắt buộc — xem tất cả

            // ── Cache key bao gồm userId để tránh cache leakage ───────────────
            var cacheKey = InternshipGroupCacheKeys.GroupList(
                request.PageNumber,
                request.PageSize,
                request.SearchTerm,
                request.Status.HasValue ? (int)request.Status.Value : null,
                request.PhaseId,
                request.IncludeArchived,
                request.EnterpriseId,
                currentUserId);

            var cached = await _cacheService.GetAsync<PaginatedResult<GetInternshipGroupsResponse>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return Result<PaginatedResult<GetInternshipGroupsResponse>>.Success(cached);
            }

            // ── Build query ───────────────────────────────────────────────────
            var query = _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(ig => ig.Enterprise)
                .Include(ig => ig.Mentor!).ThenInclude(m => m.User!)
                .Include(ig => ig.Members)
                .Include(ig => ig.InternshipPhase)
                .AsNoTracking();

            // Filter bắt buộc theo role
            if (forcedEnterpriseId.HasValue)
                query = query.Where(x => x.EnterpriseId == forcedEnterpriseId.Value);

            if (forcedMentorId.HasValue)
                query = query.Where(x => x.MentorId == forcedMentorId.Value);

            if (forcedStudentId.HasValue)
                query = query.Where(x => x.Members.Any(m => m.StudentId == forcedStudentId.Value));

            // Filter tùy chọn từ request (chỉ SuperAdmin/SchoolAdmin mới dùng được EnterpriseId override)
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var lowerSearch = request.SearchTerm.ToLower();
                query = query.Where(x => x.GroupName.ToLower().Contains(lowerSearch) ||
                                         (x.Enterprise != null && x.Enterprise.Name.ToLower().Contains(lowerSearch)));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }
            else if (!request.IncludeArchived)
            {
                query = query.Where(x => x.Status != GroupStatus.Archived);
            }

            if (request.PhaseId.HasValue)
                query = query.Where(x => x.PhaseId == request.PhaseId.Value);

            // EnterpriseId từ request: chỉ áp dụng khi không bị force (SuperAdmin/SchoolAdmin)
            if (request.EnterpriseId.HasValue && forcedEnterpriseId == null && forcedMentorId == null && forcedStudentId == null)
                query = query.Where(x => x.EnterpriseId == request.EnterpriseId.Value);

            query = query.OrderByDescending(x => x.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var resultItems = _mapper.Map<List<GetInternshipGroupsResponse>>(entities);
            var paginatedResult = new PaginatedResult<GetInternshipGroupsResponse>(resultItems, totalCount, request.PageNumber, request.PageSize);

            await _cacheService.SetAsync(cacheKey, paginatedResult, InternshipGroupCacheKeys.Expiration.GroupList, cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRetrievedGroups), resultItems.Count, currentUserId, role);

            return Result<PaginatedResult<GetInternshipGroupsResponse>>.Success(paginatedResult);
        }
    }
}

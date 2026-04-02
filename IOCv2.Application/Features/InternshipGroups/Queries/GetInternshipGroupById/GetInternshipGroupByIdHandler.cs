using AutoMapper;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IOCv2.Application.Constants;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById
{
    public class GetInternshipGroupByIdHandler : IRequestHandler<GetInternshipGroupByIdQuery, Result<GetInternshipGroupByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetInternshipGroupByIdHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;

        public GetInternshipGroupByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetInternshipGroupByIdHandler> logger,
            ICacheService cacheService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<GetInternshipGroupByIdResponse>> Handle(GetInternshipGroupByIdQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<GetInternshipGroupByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var role = _currentUserService.Role ?? string.Empty;

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogQueryById), request.InternshipId, currentUserId, role);

            var cacheKey = InternshipGroupCacheKeys.Group(request.InternshipId);
            var cached = await _cacheService.GetAsync<GetInternshipGroupByIdResponse>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                // Kiểm tra quyền ngay cả khi lấy từ cache
                var accessError = await CheckAccessAsync(role, currentUserId, cached.EnterpriseId, cached.MentorId, cached.InternshipId, cancellationToken);
                if (accessError != null) return accessError;
                return Result<GetInternshipGroupByIdResponse>.Success(cached);
            }

            var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(ig => ig.Enterprise)
                .Include(ig => ig.Mentor!).ThenInclude(m => m.User!)
                .Include(ig => ig.Members).ThenInclude(m => m.Student!).ThenInclude(s => s.User!).ThenInclude(u => u.UniversityUser!).ThenInclude(uu => uu.University!)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

            if (entity == null)
            {
                return Result<GetInternshipGroupByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            }

            // ── Kiểm tra quyền truy cập theo role ────────────────────────────
            var forbiddenResult = await CheckEntityAccessAsync(role, currentUserId, entity, cancellationToken);
            if (forbiddenResult != null) return forbiddenResult;

            var result = _mapper.Map<GetInternshipGroupByIdResponse>(entity);

            // Sắp xếp lại danh sách theo Leader lên đầu
            if (result.Members != null && result.Members.Any())
            {
                result.Members = result.Members.OrderByDescending(m => m.Role == Domain.Enums.InternshipRole.Leader).ToList();
            }

            await _cacheService.SetAsync(cacheKey, result, InternshipGroupCacheKeys.Expiration.Group, cancellationToken);

            return Result<GetInternshipGroupByIdResponse>.Success(result);
        }

        /// <summary>
        /// Kiểm tra quyền truy cập entity InternshipGroup theo role.
        /// Trả về null nếu được phép, trả về Result.Failure nếu bị từ chối.
        /// </summary>
        private async Task<Result<GetInternshipGroupByIdResponse>?> CheckEntityAccessAsync(
            string role, Guid currentUserId, InternshipGroup entity, CancellationToken cancellationToken)
        {
            if (string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "EnterpriseAdmin", StringComparison.OrdinalIgnoreCase))
            {
                // HR/EnterpriseAdmin: chỉ xem được nhóm của enterprise mình
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

                if (enterpriseUser == null || entity.EnterpriseId != enterpriseUser.EnterpriseId)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAccessDeniedHrEnterprise), currentUserId, entity.InternshipId);
                    return Result<GetInternshipGroupByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }
            else if (string.Equals(role, "Mentor", StringComparison.OrdinalIgnoreCase))
            {
                // Mentor: chỉ xem được nhóm mà mình là mentor
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

                if (enterpriseUser == null || entity.MentorId != enterpriseUser.EnterpriseUserId)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAccessDeniedMentor), currentUserId, entity.InternshipId);
                    return Result<GetInternshipGroupByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }
            else if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                // Student: chỉ xem được nhóm mà mình là thành viên
                var student = await _unitOfWork.Repository<Student>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

                if (student == null || !entity.Members.Any(m => m.StudentId == student.StudentId))
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAccessDeniedStudent), currentUserId, entity.InternshipId);
                    return Result<GetInternshipGroupByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }
            // SuperAdmin / SchoolAdmin: không giới hạn

            return null;
        }

        /// <summary>
        /// Kiểm tra quyền từ cached response (dùng EnterpriseId, MentorId, InternshipId từ DTO).
        /// </summary>
        private async Task<Result<GetInternshipGroupByIdResponse>?> CheckAccessAsync(
            string role, Guid currentUserId, Guid? enterpriseId, Guid? mentorId, Guid internshipId, CancellationToken cancellationToken)
        {
            if (string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "EnterpriseAdmin", StringComparison.OrdinalIgnoreCase))
            {
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

                if (enterpriseUser == null || enterpriseId != enterpriseUser.EnterpriseId)
                {
                    return Result<GetInternshipGroupByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }
            else if (string.Equals(role, "Mentor", StringComparison.OrdinalIgnoreCase))
            {
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

                if (enterpriseUser == null || mentorId != enterpriseUser.EnterpriseUserId)
                {
                    return Result<GetInternshipGroupByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }
            else if (string.Equals(role, "Student", StringComparison.OrdinalIgnoreCase))
            {
                // Với Student từ cache: cần hit DB lại để xác nhận membership
                // Vì cache DTO không chứa danh sách StudentId → query lại entity
                var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(ig => ig.Members)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.InternshipId == internshipId, cancellationToken);

                var student = await _unitOfWork.Repository<Student>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

                if (entity == null || student == null || !entity.Members.Any(m => m.StudentId == student.StudentId))
                {
                    return Result<GetInternshipGroupByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }
            }

            return null;
        }
    }
}

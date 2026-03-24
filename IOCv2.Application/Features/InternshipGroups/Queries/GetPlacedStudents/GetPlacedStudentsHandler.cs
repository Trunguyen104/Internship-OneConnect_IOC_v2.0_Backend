using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetPlacedStudents
{
    public class GetPlacedStudentsHandler : IRequestHandler<GetPlacedStudentsQuery, Result<PaginatedResult<GetPlacedStudentsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetPlacedStudentsHandler> _logger;

        public GetPlacedStudentsHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<GetPlacedStudentsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<GetPlacedStudentsResponse>>> Handle(GetPlacedStudentsQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<PaginatedResult<GetPlacedStudentsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            // ── 1. Xác định EnterpriseId của HR hiện tại ─────────────────────────
            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
            {
                return Result<PaginatedResult<GetPlacedStudentsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            var enterpriseId = enterpriseUser.EnterpriseId;

            // ── 2. Xác định danh sách TermId cần hiển thị ────────────────────────
            List<Guid> resolvedTermIds;

            if (request.TermId.HasValue)
            {
                // Trường hợp FE truyền TermId cụ thể
                resolvedTermIds = new List<Guid> { request.TermId.Value };
            }
            else
            {
                // Tự động tìm kỳ: ưu tiên kỳ Active → fallback kỳ Upcoming gần nhất
                resolvedTermIds = await ResolveTermIdsAsync(enterpriseId, cancellationToken);

                if (resolvedTermIds.Count == 0)
                {
                    _logger.LogInformation("No Active or Upcoming terms found for enterprise {EnterpriseId}", enterpriseId);
                    // Trả về empty result thay vì lỗi (empty state theo AC)
                    return Result<PaginatedResult<GetPlacedStudentsResponse>>.Success(
                        new PaginatedResult<GetPlacedStudentsResponse>(new List<GetPlacedStudentsResponse>(), 0, request.PageNumber, request.PageSize));
                }
            }

            _logger.LogInformation("Resolved {Count} term(s) for enterprise {EnterpriseId}: [{TermIds}]",
                resolvedTermIds.Count, enterpriseId, string.Join(", ", resolvedTermIds));

            // ── 3. Build query chính ──────────────────────────────────────────────
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = from app in _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                        where app.EnterpriseId == enterpriseId
                           && resolvedTermIds.Contains(app.TermId)
                           && app.Status == InternshipApplicationStatus.Approved

                        // Tìm nhóm đã được phân công (nếu có) của sinh viên trong cùng enterprise & term
                        let assignedGroupMember = _unitOfWork.Repository<InternshipStudent>().Query()
                            .Where(m => m.StudentId == app.StudentId
                                     && m.InternshipGroup != null
                                     && m.InternshipGroup.EnterpriseId == enterpriseId
                                     && resolvedTermIds.Contains(m.InternshipGroup.TermId)
                                     && m.InternshipGroup.Status == GroupStatus.Active)
                            .FirstOrDefault()

                        select new
                        {
                            App = app,
                            Student = app.Student,
                            User = app.Student!.User,
                            Term = app.Term,
                            AssignedGroup = assignedGroupMember != null ? assignedGroupMember.InternshipGroup : null
                        };

            // ── 4. Filter tùy chọn ───────────────────────────────────────────────
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var lowerSearch = request.SearchTerm.ToLower();
                query = query.Where(q =>
                    q.User!.FullName.ToLower().Contains(lowerSearch) ||
                    q.User.UserCode.ToLower().Contains(lowerSearch) ||
                    (q.User.Email != null && q.User.Email.ToLower().Contains(lowerSearch)));
            }

            if (request.IsAssignedToGroup.HasValue)
            {
                query = request.IsAssignedToGroup.Value
                    ? query.Where(q => q.AssignedGroup != null)
                    : query.Where(q => q.AssignedGroup == null);
            }

            // ── 5. Pagination ─────────────────────────────────────────────────────
            var totalCount = await query.CountAsync(cancellationToken);

            var pagedData = await query
                .OrderByDescending(q => q.Term.StartDate)   // Kỳ mới nhất lên trước
                .ThenByDescending(q => q.App.AppliedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(q => new GetPlacedStudentsResponse
                {
                    // Thông tin sinh viên
                    StudentId = q.Student!.StudentId,
                    StudentCode = q.User!.UserCode,
                    FullName = q.User.FullName,
                    Email = q.User.Email ?? string.Empty,
                    Major = q.Student.Major ?? string.Empty,
                    ClassName = q.Student.ClassName ?? string.Empty,
                    UniversityName = q.User.UniversityUser != null && q.User.UniversityUser.University != null
                        ? q.User.UniversityUser.University.Name
                        : null,

                    // Thông tin nhóm
                    IsAssignedToGroup = q.AssignedGroup != null,
                    AssignedGroupId = q.AssignedGroup != null ? (Guid?)q.AssignedGroup.InternshipId : null,
                    AssignedGroupName = q.AssignedGroup != null ? q.AssignedGroup.GroupName : null,
                    MentorName = q.AssignedGroup != null
                        && q.AssignedGroup.Mentor != null
                        && q.AssignedGroup.Mentor.User != null
                        ? q.AssignedGroup.Mentor.User.FullName
                        : null,

                    // Thông tin kỳ
                    TermId = q.Term.TermId,
                    TermName = q.Term.Name,
                    TermStartDate = q.Term.StartDate,
                    TermEndDate = q.Term.EndDate,
                    // Tính TermStatus inline (EF Core translatable)
                    TermStatus = q.Term.Status == TermStatus.Closed ? "Closed"
                        : q.Term.StartDate > today ? "Upcoming"
                        : q.Term.EndDate < today ? "Ended"
                        : "Active"
                })
                .ToListAsync(cancellationToken);

            var paginatedResult = new PaginatedResult<GetPlacedStudentsResponse>(pagedData, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetPlacedStudentsResponse>>.Success(paginatedResult);
        }

        /// <summary>
        /// Tìm danh sách TermId cần hiển thị:
        /// 1. Ưu tiên tất cả kỳ Active mà enterprise có sinh viên Approved
        /// 2. Fallback: kỳ Upcoming gần nhất nếu không có kỳ Active
        /// </summary>
        private async Task<List<Guid>> ResolveTermIdsAsync(Guid enterpriseId, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Lấy tất cả TermId có ít nhất 1 InternshipApplication Approved cho enterprise này
            var candidateTermIds = await _unitOfWork.Repository<InternshipApplication>().Query()
                .AsNoTracking()
                .Where(a => a.EnterpriseId == enterpriseId && a.Status == InternshipApplicationStatus.Approved)
                .Select(a => a.TermId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (candidateTermIds.Count == 0)
                return new List<Guid>();

            // Tìm kỳ Active trong danh sách ứng viên
            var activeTermIds = await _unitOfWork.Repository<Term>().Query()
                .AsNoTracking()
                .Where(t => candidateTermIds.Contains(t.TermId)
                         && t.Status == TermStatus.Open
                         && t.StartDate <= today
                         && t.EndDate >= today)
                .Select(t => t.TermId)
                .ToListAsync(cancellationToken);

            if (activeTermIds.Count > 0)
                return activeTermIds;

            // Fallback: kỳ Upcoming gần nhất
            var nearestUpcomingTermId = await _unitOfWork.Repository<Term>().Query()
                .AsNoTracking()
                .Where(t => candidateTermIds.Contains(t.TermId)
                         && t.Status == TermStatus.Open
                         && t.StartDate > today)
                .OrderBy(t => t.StartDate)
                .Select(t => t.TermId)
                .FirstOrDefaultAsync(cancellationToken);

            return nearestUpcomingTermId != Guid.Empty
                ? new List<Guid> { nearestUpcomingTermId }
                : new List<Guid>();
        }
    }
}

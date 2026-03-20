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

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    public class AddStudentsToGroupHandler : IRequestHandler<AddStudentsToGroupCommand, Result<AddStudentsToGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<AddStudentsToGroupHandler> _logger;
        private readonly ICacheService _cacheService;

        public AddStudentsToGroupHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            ILogger<AddStudentsToGroupHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<AddStudentsToGroupResponse>> Handle(AddStudentsToGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAddingStudents), request.InternshipId);

            try
            {
                // ── 1. Kiểm tra user hiện tại hợp lệ ──────────────────────────────
                if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipApplication.LogInvalidUserId));
                    return Result<AddStudentsToGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                        ResultErrorType.Unauthorized);
                }

                // ── 2. Kiểm tra user là EnterpriseUser (HR/EnterpriseAdmin) ────────
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

                if (enterpriseUser == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound));
                    return Result<AddStudentsToGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                        ResultErrorType.Forbidden);
                }

                // ── 3. Lấy nhóm (bao gồm Members để check trùng) ─────────────────
                var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (group == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<AddStudentsToGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                if (group.Status != GroupStatus.Active)
                {
                    _logger.LogWarning("Cannot add students. Group {GroupId} is not Active.", group.InternshipId);
                    return Result<AddStudentsToGroupResponse>.Failure(
                        "Chỉ có thể thêm sinh viên vào nhóm đang hoạt động (Active).",
                        ResultErrorType.BadRequest);
                }

                var studentIds = request.Students.Select(s => s.StudentId).Distinct().ToList();

                // ── 4. Kiểm tra sinh viên tồn tại trong hệ thống ──────────────────
                var existingStudents = await _unitOfWork.Repository<Student>()
                    .FindAsync(s => studentIds.Contains(s.StudentId), cancellationToken);
                var existingStudentIds = existingStudents.Select(s => s.StudentId).ToList();

                if (existingStudentIds.Count != studentIds.Count)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogInvalidStudentIds));
                    return Result<AddStudentsToGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.InvalidStudentId),
                        ResultErrorType.BadRequest);
                }

                // ── 5. Kiểm tra mỗi sinh viên đã Approved vào đúng doanh nghiệp ───
                var approvedApps = await _unitOfWork.Repository<IOCv2.Domain.Entities.InternshipApplication>().Query()
                    .AsNoTracking()
                    .Where(a => studentIds.Contains(a.StudentId)
                             && a.EnterpriseId == enterpriseUser.EnterpriseId
                             && a.Status == InternshipApplicationStatus.Approved
                             && a.TermId == group.TermId)
                    .Select(a => a.StudentId)
                    .ToListAsync(cancellationToken);

                var notApproved = studentIds.Except(approvedApps).ToList();
                if (notApproved.Any())
                {
                    var firstNotApproved = notApproved.First();
                    _logger.LogWarning(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.LogStudentNotApproved),
                        firstNotApproved, enterpriseUser.EnterpriseId);
                    return Result<AddStudentsToGroupResponse>.Failure(
                        string.Format(_messageService.GetMessage(MessageKeys.InternshipGroups.StudentNotApproved), firstNotApproved),
                        ResultErrorType.BadRequest);
                }

                // ── 5.5. Kiểm tra sinh viên đã nằm trong nhóm Active nào khác trong cùng kỳ chưa ──
                var alreadyInGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .AsNoTracking()
                    .Include(g => g.Members)
                    .Where(g => g.TermId == group.TermId && g.Status == GroupStatus.Active && g.InternshipId != group.InternshipId)
                    .SelectMany(g => g.Members)
                    .Where(m => studentIds.Contains(m.StudentId))
                    .Select(m => m.StudentId)
                    .ToListAsync(cancellationToken);

                if (alreadyInGroup.Any())
                {
                    var firstInGroup = alreadyInGroup.First();
                    _logger.LogWarning("Student {StudentId} is already in another active group in term {TermId}", firstInGroup, group.TermId);
                    return Result<AddStudentsToGroupResponse>.Failure(
                        "Sinh viên đã tham gia một nhóm khác trong kỳ này.",
                        ResultErrorType.BadRequest);
                }

                // ── 6. Thêm sinh viên vào nhóm ────────────────────────────────────
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                foreach (var item in request.Students)
                {
                    group.AddMember(item.StudentId, item.Role);
                }

                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(group);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved >= 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(group.InternshipId), cancellationToken);
                    await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAddedStudentsSuccess), studentIds.Count, request.InternshipId);

                    var response = _mapper.Map<AddStudentsToGroupResponse>(group);
                    return Result<AddStudentsToGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAddStudentsFailed));
                return Result<AddStudentsToGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogAddStudentsError));
                return Result<AddStudentsToGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}

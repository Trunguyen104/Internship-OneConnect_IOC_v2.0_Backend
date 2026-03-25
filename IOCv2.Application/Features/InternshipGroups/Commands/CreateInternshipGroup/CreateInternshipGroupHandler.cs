using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup
{
    public class CreateInternshipGroupHandler : IRequestHandler<CreateInternshipGroupCommand, Result<CreateInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateInternshipGroupHandler> _logger;
        private readonly ICacheService _cacheService;

        public CreateInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            ILogger<CreateInternshipGroupHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<CreateInternshipGroupResponse>> Handle(CreateInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogCreating), request.GroupName, request.TermId);

            try
            {
                // ── 1. Kiểm tra user hiện tại hợp lệ ──────────────────────────────
                if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipApplication.LogInvalidUserId));
                    return Result<CreateInternshipGroupResponse>.Failure(
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
                    return Result<CreateInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                        ResultErrorType.Forbidden);
                }

                // ── 3. Validate TermId & kiểm tra kỳ phải đang Active ─────────────
                var term = await _unitOfWork.Repository<Term>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

                if (term == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogTermNotFound), request.TermId);
                    return Result<CreateInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.TermNotFound),
                        ResultErrorType.NotFound);
                }

                if (!TermStatusHelper.IsActive(term.StartDate, term.EndDate, term.Status))
                {
                    _logger.LogWarning("Term {TermId} is not active (status: {Status}, start: {Start}, end: {End}). Cannot create internship group.",
                        request.TermId, term.Status, term.StartDate, term.EndDate);
                    return Result<CreateInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.TermNotActive),
                        ResultErrorType.BadRequest);
                }

                // ── 4. Validate EnterpriseId: phải là công ty của HR hiện tại ──────
                if (request.EnterpriseId.HasValue)
                {
                    if (request.EnterpriseId.Value != enterpriseUser.EnterpriseId)
                    {
                        _logger.LogWarning("HR {UserId} attempted to create group for enterprise {EnterpriseId} which is not their own.",
                            currentUserId, request.EnterpriseId.Value);
                        return Result<CreateInternshipGroupResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseNotFound),
                            ResultErrorType.Forbidden);
                    }
                }

                // ── 5. Validate MentorId: request truyền UserId của mentor ────────────
                Guid? resolvedMentorId = null; // EnterpriseUserId lưu vào DB
                if (request.MentorId.HasValue)
                {
                    // Tìm EnterpriseUser theo UserId (frontend truyền UserId, không phải EnterpriseUserId)
                    var mentor = await _unitOfWork.Repository<EnterpriseUser>()
                        .Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == request.MentorId.Value
                                               && u.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);
                    if (mentor == null)
                    {
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogMentorNotFound), request.MentorId);
                        return Result<CreateInternshipGroupResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound),
                            ResultErrorType.NotFound);
                    }
                    resolvedMentorId = mentor.EnterpriseUserId; // Lưu EnterpriseUserId vào DB
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var newGroup = InternshipGroup.Create(
                    request.TermId,
                    request.GroupName,
                    request.Description,
                    request.EnterpriseId,
                    resolvedMentorId, // EnterpriseUserId
                    request.StartDate,
                    request.EndDate
                );

                // ── 6. Bắt buộc ít nhất 1 sinh viên ───────────────────────────────
                if (request.Students == null || !request.Students.Any())
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNoStudentsProvided), request.GroupName);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<CreateInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.AtLeastOneStudentRequired),
                        ResultErrorType.BadRequest);
                }

                // ── 7. Xử lý thêm sinh viên ───────────────────────────────
                var studentIds = request.Students.Select(s => s.StudentId).Distinct().ToList();

                    // 6a. Kiểm tra sinh viên tồn tại trong hệ thống
                    var existingStudents = await _unitOfWork.Repository<Student>()
                        .FindAsync(u => studentIds.Contains(u.StudentId), cancellationToken);
                    var existingStudentIds = existingStudents.Select(u => u.StudentId).ToList();

                    if (existingStudentIds.Count != studentIds.Count)
                    {
                        var missingId = studentIds.Except(existingStudentIds).First();
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogStudentNotFound), missingId);
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<CreateInternshipGroupResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.StudentNotFound),
                            ResultErrorType.NotFound);
                    }

                    // 6b. Kiểm tra mỗi sinh viên đã được Approved vào đúng doanh nghiệp của HR
                    var approvedApps = await _unitOfWork.Repository<IOCv2.Domain.Entities.InternshipApplication>().Query()
                        .AsNoTracking()
                        .Where(a => studentIds.Contains(a.StudentId)
                                 && a.EnterpriseId == enterpriseUser.EnterpriseId
                                 && a.Status == InternshipApplicationStatus.Placed
                                 && a.TermId == request.TermId)
                        .Select(a => a.StudentId)
                        .ToListAsync(cancellationToken);

                    var notApproved = studentIds.Except(approvedApps).ToList();
                    if (notApproved.Any())
                    {
                        var firstNotApproved = notApproved.First();
                        _logger.LogWarning(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.LogStudentNotApproved),
                            firstNotApproved, enterpriseUser.EnterpriseId);
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<CreateInternshipGroupResponse>.Failure(
                            string.Format(_messageService.GetMessage(MessageKeys.InternshipGroups.StudentNotApproved), firstNotApproved),
                            ResultErrorType.BadRequest);
                    }

                    // 6c. Kiểm tra sinh viên đã nằm trong nhóm Active nào khác trong cùng kỳ chưa
                    var alreadyInGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                        .AsNoTracking()
                        .Include(g => g.Members)
                        .Where(g => g.TermId == request.TermId && g.Status == GroupStatus.Active)
                        .SelectMany(g => g.Members)
                        .Where(m => studentIds.Contains(m.StudentId))
                        .Select(m => m.StudentId)
                        .ToListAsync(cancellationToken);

                    if (alreadyInGroup.Any())
                    {
                        var firstInGroup = alreadyInGroup.First();
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogStudentAlreadyInActiveGroup), firstInGroup, request.TermId);
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<CreateInternshipGroupResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.StudentAlreadyInActiveGroup),
                            ResultErrorType.BadRequest);
                    }

                    foreach (var studentRef in request.Students)
                    {
                        newGroup.AddMember(studentRef.StudentId, studentRef.Role);
                    }

                await _unitOfWork.Repository<InternshipGroup>().AddAsync(newGroup);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogCreatedSuccess), newGroup.InternshipId);

                    var response = _mapper.Map<CreateInternshipGroupResponse>(newGroup);
                    return Result<CreateInternshipGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(_messageService.GetMessage(MessageKeys.InternshipGroups.LogCreationFailed));
                return Result<CreateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogCreationError));
                return Result<CreateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}

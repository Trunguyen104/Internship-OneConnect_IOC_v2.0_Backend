using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
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

        public CreateInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            ILogger<CreateInternshipGroupHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
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

                // ── 3. Validate TermId ─────────────────────────────────────────────
                var termExists = await _unitOfWork.Repository<Term>()
                    .ExistsAsync(t => t.TermId == request.TermId, cancellationToken);
                if (!termExists)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogTermNotFound), request.TermId);
                    return Result<CreateInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.TermNotFound),
                        ResultErrorType.NotFound);
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

                // ── 5. Validate MentorId: phải thuộc cùng công ty của HR ────────────
                if (request.MentorId.HasValue)
                {
                    var mentorBelongsToEnterprise = await _unitOfWork.Repository<EnterpriseUser>()
                        .ExistsAsync(u => u.EnterpriseUserId == request.MentorId.Value
                                       && u.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);
                    if (!mentorBelongsToEnterprise)
                    {
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogMentorNotFound), request.MentorId);
                        return Result<CreateInternshipGroupResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound),
                            ResultErrorType.NotFound);
                    }
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var newGroup = InternshipGroup.Create(
                    request.TermId,
                    request.GroupName,
                    request.EnterpriseId,
                    request.MentorId,
                    request.StartDate,
                    request.EndDate
                );

                // ── 6. Nếu có sinh viên → kiểm tra tồn tại + đã được Approved ────
                // (Students là tuỳ chọn — có thể tạo nhóm trống rồi thêm sau)
                if (request.Students != null && request.Students.Any())
                {
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
                                 && a.Status == InternshipApplicationStatus.Approved
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

                    foreach (var studentRef in request.Students)
                    {
                        newGroup.AddMember(studentRef.StudentId, studentRef.Role);
                    }
                }

                await _unitOfWork.Repository<InternshipGroup>().AddAsync(newGroup);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
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
                return Result<CreateInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}

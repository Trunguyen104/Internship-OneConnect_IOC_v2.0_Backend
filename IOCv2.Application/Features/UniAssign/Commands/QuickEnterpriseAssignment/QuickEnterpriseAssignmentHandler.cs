using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.UniAssigns;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Commands.QuickEnterpriseAssignment
{
    internal class QuickEnterpriseAssignmentHandler : IRequestHandler<QuickEnterpriseAssignmentCommand, Result<QuickEnterpriseAssignmentResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<QuickEnterpriseAssignmentHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IPublisher _publisher;

        public QuickEnterpriseAssignmentHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<QuickEnterpriseAssignmentHandler> logger, IMessageService messageService, IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
            _messageService = messageService;
            _publisher = publisher;
        }

        public async Task<Result<QuickEnterpriseAssignmentResponse>> Handle(QuickEnterpriseAssignmentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = Guid.Parse(_currentUser.UserId!);
            if (UniAssignParam.CreateUniAssignParam.UniAllowedRole.Contains(_currentUser.Role))
            {
                var currentUserUniversityId = await _unitOfWork.Repository<UniversityUser>().Query().Where(x => x.UserId == currentUserId).Select(x => x.UniversityId).FirstOrDefaultAsync(cancellationToken);
                var studentUser = await _unitOfWork.Repository<Student>().Query()
    .Where(x => x.StudentId == request.StudentId)
    .Include(x => x.User)
    .Select(x => new { x.UserId, InternshipStatus = x.InternshipStatus, FullName = x.User.FullName })
    .FirstOrDefaultAsync(cancellationToken);
                if (studentUser == null) return Result<QuickEnterpriseAssignmentResponse>.Failure("Student not found.", ResultErrorType.NotFound);
                if (studentUser.InternshipStatus == StudentStatus.Placed) return Result<QuickEnterpriseAssignmentResponse>.Failure("Student has already been placed in an internship.", ResultErrorType.BadRequest);
                var studentUniversityId = await _unitOfWork.Repository<UniversityUser>().Query().Where(x => x.UserId == studentUser.UserId).Select(x => x.UniversityId).FirstOrDefaultAsync(cancellationToken);
                if (currentUserUniversityId != studentUniversityId)
                    return Result<QuickEnterpriseAssignmentResponse>.Failure("You are not allowed to assign this student.", ResultErrorType.Unauthorized);
            }

            var term = await _unitOfWork.Repository<Term>().GetByIdAsync(request.TermId, cancellationToken);
            if (term == null) return Result<QuickEnterpriseAssignmentResponse>.Failure("Term not found.", ResultErrorType.NotFound);
            if (term.Status != TermStatus.Open) return Result<QuickEnterpriseAssignmentResponse>.Failure("Term is not open for assignment.", ResultErrorType.BadRequest);

            var internshipPhase = await _unitOfWork.Repository<InternshipPhase>().Query().Where(x => x.PhaseId == request.InternPhaseId).Include(ia => ia.Jobs)
                .ThenInclude(j => j.InternshipApplications).Include(i => i.Enterprise).Select(x => new { x.PhaseId, x.EnterpriseId, x.StartDate, x.EndDate, x.Capacity, x.Enterprise!.Name, x.Jobs }).FirstOrDefaultAsync(cancellationToken);
            
            if (internshipPhase == null) return Result<QuickEnterpriseAssignmentResponse>.Failure("Internship phase not found.", ResultErrorType.NotFound);

            // Validate phase date ordering
            if (internshipPhase.StartDate > internshipPhase.EndDate)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Internship phase start date is after its end date.", ResultErrorType.BadRequest);
            }
            // Ensure the internship phase is within the term date range
            if (internshipPhase.StartDate < term.StartDate || internshipPhase.EndDate > term.EndDate)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Internship phase dates must be within the term start and end dates.", ResultErrorType.BadRequest);
            }
            // Ensure the internship phase has at least one job posting with status Published or Closed.
            var hasJobPosting = await _unitOfWork.Repository<Job>().Query()
                .Where(j => j.InternshipPhaseId == internshipPhase.PhaseId && (j.Status == JobStatus.PUBLISHED || j.Status == JobStatus.CLOSED))
                .AnyAsync(cancellationToken);
            if (!hasJobPosting)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Internship phase must have at least one published or closed job posting.", ResultErrorType.BadRequest);
            }

            var remainingCapacity = internshipPhase.Capacity - internshipPhase.Jobs.SelectMany(j => j.InternshipApplications).Count(ia => ia.Status == InternshipApplicationStatus.Placed);
            if (remainingCapacity <= 0)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Intern Phase này vừa đủ số lượng nhận. Vui lòng chọn phase hoặc doanh nghiệp khác.", ResultErrorType.BadRequest);
            }

            // AC-11: Hard-block if student already has an active self-apply application at this enterprise (Applied / Interviewing / Offered)
            var activeSelfApplyStatuses = new[] { InternshipApplicationStatus.Applied, InternshipApplicationStatus.Interviewing, InternshipApplicationStatus.Offered };
            var existingSelfApply = await _unitOfWork.Repository<InternshipApplication>().Query()
                .Include(a => a.Enterprise)
                .Where(a =>
                    a.StudentId == request.StudentId &&
                    a.EnterpriseId == request.EnterpriseId &&
                    a.TermId == request.TermId &&
                    a.Source == ApplicationSource.SelfApply &&
                    activeSelfApplyStatuses.Contains(a.Status))
                .FirstOrDefaultAsync(cancellationToken);

            var student = await _unitOfWork.Repository<Student>().Query()
    .Where(x => x.StudentId == request.StudentId)
    .Include(x => x.User)
    .Select(x => new { x.UserId, FullName = x.User.FullName })
    .FirstOrDefaultAsync(cancellationToken);
            var studentName = student?.FullName;

            if (existingSelfApply != null)
            {
                var statusLabel = existingSelfApply.Status.ToString();
                var enterpriseName = existingSelfApply.Enterprise?.Name ?? (await _unitOfWork.Repository<Enterprise>().Query().Where(e => e.EnterpriseId == request.EnterpriseId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken)) ?? string.Empty;
                var blockMsg = $"Sinh viên {studentName} đang có đơn tự ứng tuyển đang xử lý tại {enterpriseName} (trạng thái: {statusLabel}). Vui lòng chọn doanh nghiệp khác hoặc yêu cầu sinh viên rút đơn trước.";
                _logger.LogInformation("Hard-block quick assign for student {StudentId} due to existing self-apply application at enterprise {EnterpriseId}.", request.StudentId, request.EnterpriseId);
                return Result<QuickEnterpriseAssignmentResponse>.Failure(blockMsg, ResultErrorType.Conflict);
            }

            // QE-01: Hard-block if student has any internship data (logbook / sprint work items / evaluations) from a previous term
            // We determine "previous term" by comparing the internship's phase start date with the current term start date.
            var termStart = term.StartDate;

            var hasLogbookFromPrevious = await _unitOfWork.Repository<Logbook>().Query()
                .Include(l => l.Internship).ThenInclude(ig => ig.InternshipPhase)
                .Where(l => l.StudentId == request.StudentId && l.Internship.InternshipPhase.StartDate < termStart)
                .AnyAsync(cancellationToken);

            var hasSprintWorkItemFromPrevious = await _unitOfWork.Repository<SprintWorkItem>().Query()
                .Include(swi => swi.WorkItem)
                .Include(swi => swi.Sprint).ThenInclude(s => s.Project).ThenInclude(p => p.InternshipGroup).ThenInclude(ig => ig!.InternshipPhase)
                .Where(swi => swi.WorkItem.AssigneeId == request.StudentId && swi.Sprint.Project.InternshipGroup!.InternshipPhase.StartDate < termStart)
                .AnyAsync(cancellationToken);

            var hasEvaluationFromPrevious = await _unitOfWork.Repository<Evaluation>().Query()
                .Include(e => e.Internship).ThenInclude(ig => ig.InternshipPhase)
                .Where(e => e.StudentId == request.StudentId && e.Internship.InternshipPhase.StartDate < termStart)
                .AnyAsync(cancellationToken);

            if (hasLogbookFromPrevious || hasSprintWorkItemFromPrevious || hasEvaluationFromPrevious)
            {
                _logger.LogInformation("Hard-block quick assign for student {StudentId} due to prior internship data (logbook/sprint/evaluation) from previous term.", request.StudentId);
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Không thể assign. Sinh viên đã có dữ liệu thực tập từ kỳ trước.", ResultErrorType.Conflict);
            }

            var internshipApplication = new InternshipApplication
            {
                ApplicationId = Guid.NewGuid(),
                EnterpriseId = request.EnterpriseId,
                TermId = request.TermId,
                StudentId = request.StudentId,
                InternPhaseId = request.InternPhaseId,
                Status = InternshipApplicationStatus.PendingAssignment,
                Source = ApplicationSource.UniAssign,
                AppliedAt = DateTime.UtcNow
            };
            try
            {
                // collect audit logs to persist atomically with the application creation
                var auditLogs = new System.Collections.Generic.List<AuditLog>();

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                await _unitOfWork.Repository<InternshipApplication>().AddAsync(internshipApplication, cancellationToken);

                // create audit log for this assign action
                auditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Assign,
                    EntityType = nameof(InternshipApplication),
                    EntityId = internshipApplication.ApplicationId,
                    PerformedById = currentUserId,
                    Metadata = $"{{\"studentId\":\"{request.StudentId}\",\"enterpriseId\":\"{request.EnterpriseId}\",\"internPhaseId\":\"{request.InternPhaseId}\",\"termId\":\"{request.TermId}\",\"note\":\"quick assign - created pending\"}}"
                });

                if (auditLogs.Any())
                {
                    await _unitOfWork.Repository<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
                }

                // Save and commit: handle optimistic concurrency specifically
                try
                {
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException dbEx)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogWarning(dbEx, "Concurrency conflict while creating internship application for student {StudentId} in term {TermId} and enterprise {EnterpriseId}", request.StudentId, request.TermId, request.EnterpriseId);
                    // Return a conflict so the UI can show a toast/warning to the user and prompt refresh
                    return Result<QuickEnterpriseAssignmentResponse>.Failure("Đã xảy ra xung đột đồng thời. Một người khác vừa gán sinh viên này. Vui lòng tải lại trang và thử lại.", ResultErrorType.Conflict);
                }

                // Notify student (AC-07) — send in-app notification about assignment (Pending)
                if (student?.UserId != Guid.Empty && student != null)
                {
                    var enterpriseName = internshipPhase.Name != null ? internshipPhase.Name : (await _unitOfWork.Repository<Enterprise>().Query().Where(e => e.EnterpriseId == request.EnterpriseId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken)) ?? string.Empty;
                    // Use term.Name for term label
                    var termName = (await _unitOfWork.Repository<Term>().Query().Where(t => t.TermId == request.TermId).Select(t => t.Name).FirstOrDefaultAsync(cancellationToken)) ?? string.Empty;

                    await _publisher.Publish(new ApplicationAssignedUniAssignEvent(
                        student.UserId,
                        internshipApplication.ApplicationId,
                        enterpriseName,
                        termName), cancellationToken);
                }

                var response = new QuickEnterpriseAssignmentResponse
                {
                    Message = $"Đã gửi chỉ định [{internshipPhase.Name}] cho [{studentName}]. Đang chờ doanh nghiệp xác nhận."
                };
                return Result<QuickEnterpriseAssignmentResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while creating internship application for student {StudentId} in term {TermId} and enterprise {EnterpriseId}", request.StudentId, request.TermId, request.EnterpriseId);
                return Result<QuickEnterpriseAssignmentResponse>.Failure("An error occurred while processing the assignment. Please try again later.", ResultErrorType.InternalServerError);
            }
        }
    }
}
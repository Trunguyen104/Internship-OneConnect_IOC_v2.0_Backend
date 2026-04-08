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
using IOCv2.Application.Constants;

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
                if (studentUser == null) return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.StudentNotFound), ResultErrorType.NotFound);
                if (studentUser.InternshipStatus == StudentStatus.Placed) return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.StudentAlreadyPlaced), ResultErrorType.BadRequest);
                var studentUniversityId = await _unitOfWork.Repository<UniversityUser>().Query().Where(x => x.UserId == studentUser.UserId).Select(x => x.UniversityId).FirstOrDefaultAsync(cancellationToken);
                if (currentUserUniversityId != studentUniversityId)
                    return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.StudentsUnauthorized, studentUser.FullName), ResultErrorType.Unauthorized);
            }

            var term = await (
                    from st in _unitOfWork.Repository<StudentTerm>().Query()
                    join t in _unitOfWork.Repository<Term>().Query() on st.TermId equals t.TermId
                    where st.StudentId == request.StudentId && t.Status == TermStatus.Open
                    orderby t.StartDate descending
                    select t
                ).FirstOrDefaultAsync(cancellationToken);
            if (term == null) return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.NotFound);
            if (term.Status != TermStatus.Open) return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.NotOpenForAssignment), ResultErrorType.BadRequest);

            var internshipPhase = await _unitOfWork.Repository<InternshipPhase>().Query().Where(x => x.PhaseId == request.InternPhaseId).Include(ia => ia.Jobs)
                .ThenInclude(j => j.InternshipApplications).Include(i => i.Enterprise).Select(x => new { x.PhaseId, x.EnterpriseId, x.StartDate, x.EndDate, x.Capacity, x.Enterprise!.Name, x.Jobs }).FirstOrDefaultAsync(cancellationToken);
            
            if (internshipPhase == null) return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipPhase.NotFound), ResultErrorType.NotFound);

            // Validate phase date ordering
            if (internshipPhase.StartDate > internshipPhase.EndDate)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipPhase.StartDateAfterEndDate), ResultErrorType.BadRequest);
            }
            
            // Ensure the internship phase has at least one job posting with status Published or Closed.
            var hasJobPosting = await _unitOfWork.Repository<Job>().Query()
                .Where(j => j.InternshipPhaseId == internshipPhase.PhaseId && (j.Status == JobStatus.PUBLISHED || j.Status == JobStatus.CLOSED))
                .AnyAsync(cancellationToken);
            if (!hasJobPosting)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.NoJobPosting), ResultErrorType.BadRequest);
            }

            var remainingCapacity = internshipPhase.Capacity - internshipPhase.Jobs.SelectMany(j => j.InternshipApplications).Count(ia => ia.Status == InternshipApplicationStatus.Placed);
            if (remainingCapacity <= 0)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.InternPhaseFull), ResultErrorType.BadRequest);
            }

            // AC-11: Hard-block if student already has an active self-apply application at this enterprise (Applied / Interviewing / Offered)
            var activeSelfApplyStatuses = new[] { InternshipApplicationStatus.Applied, InternshipApplicationStatus.Interviewing, InternshipApplicationStatus.Offered };
            var existingSelfApply = await _unitOfWork.Repository<InternshipApplication>().Query()
                .Include(a => a.Enterprise)
                .Where(a =>
                    a.StudentId == request.StudentId &&
                    a.EnterpriseId == request.EnterpriseId &&
                    a.TermId == term.TermId &&
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
                var blockMsg = _messageService.GetMessage(MessageKeys.UniAssign.StudentHasPendingApplicationAtEnterprise, studentName!, enterpriseName, statusLabel);
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
                return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.StudentHasPriorInternshipData), ResultErrorType.Conflict);
            }

            var internshipApplication = new InternshipApplication
            {
                ApplicationId = Guid.NewGuid(),
                EnterpriseId = request.EnterpriseId,
                TermId = term.TermId,
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
                    Metadata = $"{{\"studentId\":\"{request.StudentId}\",\"enterpriseId\":\"{request.EnterpriseId}\",\"internPhaseId\":\"{request.InternPhaseId}\",\"termId\":\"{term.TermId}\",\"note\":\"quick assign - created pending\"}}"
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
                    _logger.LogWarning(dbEx, "Concurrency conflict while creating internship application for student {StudentId} in term {TermId} and enterprise {EnterpriseId}", request.StudentId, term.TermId, request.EnterpriseId);
                    // Return a conflict so the UI can show a toast/warning to the user and prompt refresh
                    return Result<QuickEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.ConcurrencyConflict), ResultErrorType.Conflict);
                }

                // Notify student (AC-07) — send in-app notification about assignment (Pending)
                if (student?.UserId != Guid.Empty && student != null)
                {
                    var enterpriseName = internshipPhase.Name != null ? internshipPhase.Name : (await _unitOfWork.Repository<Enterprise>().Query().Where(e => e.EnterpriseId == request.EnterpriseId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken)) ?? string.Empty;
                    // Use term.Name for term label
                    var termName = (await _unitOfWork.Repository<Term>().Query().Where(t => t.TermId == term.TermId).Select(t => t.Name).FirstOrDefaultAsync(cancellationToken)) ?? string.Empty;

                    await _publisher.Publish(new ApplicationAssignedUniAssignEvent(
                        student.UserId,
                        internshipApplication.ApplicationId,
                        enterpriseName,
                        termName), cancellationToken);
                }

                var response = new QuickEnterpriseAssignmentResponse
                {
                    Message = _messageService.GetMessage(MessageKeys.UniAssign.QuickAssignSuccess, studentName ?? "Student", internshipPhase.Name ?? "Enterprise"),
                };
                return Result<QuickEnterpriseAssignmentResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while creating internship application for student {StudentId} in term {TermId} and enterprise {EnterpriseId}", request.StudentId, term.TermId, request.EnterpriseId);
                return Result<QuickEnterpriseAssignmentResponse>.Failure(ex.Message, ResultErrorType.InternalServerError);
            }
        }
    }
}
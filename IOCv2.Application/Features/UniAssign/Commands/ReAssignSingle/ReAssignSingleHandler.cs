using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Extensions.UniAssigns;

namespace IOCv2.Application.Features.UniAssign.Commands.ReAssignSingle
{
    public class ReAssignSingleHandler : IRequestHandler<ReAssignSingleCommand, Result<ReAssignSingleResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IPublisher _publisher;
        private readonly ILogger<ReAssignSingleHandler> _logger;

        public ReAssignSingleHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IPublisher publisher,
            ILogger<ReAssignSingleHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<ReAssignSingleResponse>> Handle(ReAssignSingleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ReAssignSingle requested for StudentId {StudentId} to Enterprise {NewEnterpriseId}", request.StudentId, request.NewEnterpriseId);

            try
            {
                if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                    return Result<ReAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                        ResultErrorType.Unauthorized);

                // Resolve UniversityUser (uni admin)
                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query().AsNoTracking()
                    .FirstOrDefaultAsync(uu => uu.UserId == currentUserId, cancellationToken);

                if (universityUser == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.UniAdminInternship.LogUniversityUserNotFound), currentUserId);
                    return Result<ReAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.UniAdminInternship.UniversityUserNotFound),
                        ResultErrorType.Forbidden);
                }

                // Load application
                var app = await _unitOfWork.Repository<InternshipApplication>().Query()
                    .Include(a => a.Student).ThenInclude(s => s.User)
                    .Include(a => a.Enterprise)
                    .Include(a => a.Term)
                    .FirstOrDefaultAsync(a => a.StudentId == request.StudentId && UniAssignParam.CommonUniAssignParam.AllowedStatuses.Contains(a.Status), cancellationToken);

                if (app == null)
                    return Result<ReAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipApplication.NotFound),
                        ResultErrorType.NotFound);

                if (app.Source != ApplicationSource.UniAssign)
                    return Result<ReAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.HRApplications.NotUniAssignApplication),
                        ResultErrorType.BadRequest);

                // Allow reassign for PendingAssignment and Placed -> handle both per AC-07
                if (app.Status != InternshipApplicationStatus.Placed &&
                    app.Status != InternshipApplicationStatus.PendingAssignment)
                {
                    return Result<ReAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.HRApplications.InvalidTransition),
                        ResultErrorType.BadRequest);
                }

                // Term ended/closed check
                var term = await _unitOfWork.Repository<Term>().Query().AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TermId == app.TermId, cancellationToken);

                if (term != null && term.Status == TermStatus.Closed)
                {
                    return Result<ReAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.TermEndedOrClosed),
                        ResultErrorType.BadRequest);
                }

                var studentId = app.StudentId;
                var studentName = app.Student?.User?.FullName ?? string.Empty;
                var oldEnterpriseName = app.Enterprise?.Name ?? string.Empty;
                var oldEnterpriseId = app.EnterpriseId;

                // AC-11: Hard-block if student already has an active self-apply application at the target enterprise
                var activeSelfApplyStatuses = new[] { InternshipApplicationStatus.Applied, InternshipApplicationStatus.Interviewing, InternshipApplicationStatus.Offered };
                var existingSelfApply = await _unitOfWork.Repository<InternshipApplication>().Query()
                    .Include(a => a.Enterprise)
                    .Where(a =>
                        a.StudentId == studentId &&
                        a.EnterpriseId == request.NewEnterpriseId &&
                        a.TermId == app.TermId &&
                        a.Source == ApplicationSource.SelfApply &&
                        activeSelfApplyStatuses.Contains(a.Status))
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingSelfApply != null)
                {
                    var statusLabel = existingSelfApply.Status.ToString();
                    var enterpriseName = existingSelfApply.Enterprise?.Name ?? (await _unitOfWork.Repository<Enterprise>().Query().Where(e => e.EnterpriseId == request.NewEnterpriseId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken)) ?? string.Empty;
                    var blockMsg = _messageService.GetMessage(MessageKeys.UniAssign.StudentHasPendingApplicationAtEnterprise, studentName, enterpriseName, statusLabel);
                    _logger.LogInformation("Hard-block reassign for student {StudentId} due to existing self-apply application at enterprise {EnterpriseId}.", studentId, request.NewEnterpriseId);
                    return Result<ReAssignSingleResponse>.Failure(blockMsg, ResultErrorType.Conflict);
                }

                // Capacity check for the target internship phase (RS-05)
                if (request.NewInternPhaseId != Guid.Empty)
                {
                    var phase = await _unitOfWork.Repository<InternshipPhase>().Query()
                        .Include(p => p.Jobs.Where(j => j.DeletedAt == null))
                            .ThenInclude(j => j.InternshipApplications.Where(a => a.Status == InternshipApplicationStatus.Placed))
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.PhaseId == request.NewInternPhaseId && p.DeletedAt == null, cancellationToken);

                    if (phase == null)
                    {
                        return Result<ReAssignSingleResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.InternshipPhase.NotFound),
                            ResultErrorType.NotFound);
                    }

                    // Count distinct placed students in that phase (same logic as GetInternshipPhaseById)
                    var latestPlacedApplicationsForPhase = phase.Jobs
                        .SelectMany(j => j.InternshipApplications
                            .Where(a => a.Status == InternshipApplicationStatus.Placed))
                        .GroupBy(a => a.StudentId)
                        .Select(g => g.OrderByDescending(a => a.ReviewedAt).First())
                        .ToList();

                    var placedCountForPhase = latestPlacedApplicationsForPhase.Count;
                    var remaining = Math.Max(phase.Capacity - placedCountForPhase, 0);

                    if (remaining <= 0)
                    {
                        _logger.LogInformation("Reject reassign: target intern phase {PhaseId} has no remaining capacity.", request.NewInternPhaseId);
                        return Result<ReAssignSingleResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.HRApplications.InternPhaseAtCapacity),
                            ResultErrorType.Conflict);
                    }
                }

                // Find internship groups (internshipIds) where this student has membership under the current enterprise
                var internshipIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .Include(isv => isv.InternshipGroup)
                    .Where(isv => isv.StudentId == studentId && isv.InternshipGroup.EnterpriseId == oldEnterpriseId && isv.DeletedAt == null)
                    .Select(isv => isv.InternshipId)
                    .ToListAsync(cancellationToken);

                // Check presence of Logbooks / Evaluations / Sprints and block if any exist (same as before)
                var hasLogbooks = false;
                var hasEvaluations = false;
                var hasSprints = false;

                if (internshipIds.Any())
                {
                    hasLogbooks = await _unitOfWork.Repository<Logbook>().Query()
                        .AsNoTracking()
                        .AnyAsync(l => internshipIds.Contains(l.InternshipId) && l.StudentId == studentId && l.DeletedAt == null, cancellationToken);

                    hasEvaluations = await _unitOfWork.Repository<Evaluation>().Query()
                        .AsNoTracking()
                        .AnyAsync(e => internshipIds.Contains(e.InternshipId) && e.StudentId == studentId, cancellationToken);

                    // Sprint check: sprint linked through Project -> Project.InternshipId
                    hasSprints = await _unitOfWork.Repository<Sprint>().Query()
                        .AsNoTracking()
                        .AnyAsync(s => _unitOfWork.Repository<Project>().Query()
                            .Any(p => p.ProjectId == s.ProjectId && p.InternshipId.HasValue && internshipIds.Contains(p.InternshipId!.Value)), cancellationToken);
                }

                if (hasLogbooks || hasSprints || hasEvaluations)
                {
                    var blockMsg = _messageService.GetMessage(MessageKeys.UniAssign.CannotReassignStudentHasInternshipData, studentName, oldEnterpriseName);
                    _logger.LogInformation("Hard-block reassign for student {StudentId} due to existing internship data.", studentId);
                    return Result<ReAssignSingleResponse>.Failure(blockMsg, ResultErrorType.Conflict);
                }

                // Prepare audit collection
                var auditLogs = new List<AuditLog>();

                // Handle two flows:
                // A) app.Status == PendingAssignment -> update existing application enterprise (Reassign from Pending)
                // B) app.Status == Placed -> withdraw old and create new PendingAssignment (Reassign from Placed)
                if (app.Status == InternshipApplicationStatus.PendingAssignment)
                {
                    // Update enterprise for existing pending application
                    var previousEnterpriseId = app.EnterpriseId;
                    app.EnterpriseId = request.NewEnterpriseId;
                    app.InternPhaseId = request.NewInternPhaseId;
                    app.UpdatedAt = DateTime.UtcNow;
                    app.UpdatedBy = currentUserId;

                    // history record to capture reassign action (status unchanged)
                    var history = new ApplicationStatusHistory
                    {
                        HistoryId = Guid.NewGuid(),
                        ApplicationId = app.ApplicationId,
                        FromStatus = app.Status,
                        ToStatus = app.Status,
                        Note = "UniAdmin reassign pending (AC-07)",
                        TriggerSource = "Uni",
                        ChangedByName = universityUser.UserId.ToString(),
                        CreatedAt = DateTime.UtcNow
                    };
                    await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(history, cancellationToken);

                    // Audit: mark as Assign (reassigned)
                    auditLogs.Add(new AuditLog
                    {
                        AuditLogId = Guid.NewGuid(),
                        Action = AuditAction.Assign,
                        EntityType = nameof(InternshipApplication),
                        EntityId = app.ApplicationId,
                        PerformedById = currentUserId,
                        Metadata = $"{{\"studentId\":\"{studentId}\",\"oldEnterpriseId\":\"{previousEnterpriseId}\",\"newEnterpriseId\":\"{request.NewEnterpriseId}\",\"newInternPhaseId\":\"{request.NewInternPhaseId}\",\"note\":\"reassigned pending\"}}"
                    });

                    await _unitOfWork.Repository<InternshipApplication>().UpdateAsync(app, cancellationToken);

                    // persist audit logs together
                    if (auditLogs.Any())
                    {
                        await _unitOfWork.Repository<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
                    }

                    await _unitOfWork.SaveChangeAsync(cancellationToken);

                    // Publish notification: Reassigned from Pending
                    if (app.Student?.UserId != Guid.Empty)
                    {
                        var newEnterpriseName = await _unitOfWork.Repository<Enterprise>().Query()
                            .Where(e => e.EnterpriseId == request.NewEnterpriseId)
                            .Select(e => e.Name)
                            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

                        await _publisher.Publish(new ApplicationReassignedFromPendingEvent(
                            app.Student!.UserId,
                            app.ApplicationId,
                            newEnterpriseName), cancellationToken);
                    }
                    var enterpriseName = (await _unitOfWork.Repository<Enterprise>().Query().Where(e => e.EnterpriseId == request.NewEnterpriseId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken));
                    var respPending = new ReAssignSingleResponse
                    {
                        OldApplicationId = app.ApplicationId,
                        NewApplicationId = app.ApplicationId,
                        Status = app.Status,
                        StatusLabel = app.Status.ToString(),
                        UpdatedAt = DateTime.UtcNow,
                        Message = _messageService.GetMessage(MessageKeys.UniAssign.ReassignPendingSuccess, studentName, enterpriseName!)
                    };

                    _logger.LogInformation("ReAssignSingle (pending) completed for application {ApplicationId}", app.ApplicationId);
                    return Result<ReAssignSingleResponse>.Success(respPending, respPending.Message);
                }

                // app.Status == Placed -> existing behavior: withdraw old placed app + create new pending application
                // All checks passed — perform reassign from Placed:
                // 1) Update old application -> Withdrawn
                var previousStatus = app.Status;
                var oldAppId = app.ApplicationId;
                var oldAppEnterpriseId = app.EnterpriseId;
                var previousPlacedEnterpriseName = app.Enterprise?.Name ?? string.Empty;

                app.Status = InternshipApplicationStatus.Withdrawn;
                app.UpdatedAt = DateTime.UtcNow;

                var historyOld = new ApplicationStatusHistory
                {
                    HistoryId = Guid.NewGuid(),
                    ApplicationId = app.ApplicationId,
                    FromStatus = previousStatus,
                    ToStatus = app.Status,
                    Note = "UniAdmin reassign single (AC-05)",
                    TriggerSource = "Uni",
                    ChangedByName = universityUser.UserId.ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(historyOld, cancellationToken);

                // Audit: unassign the old placed application
                auditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Unassign,
                    EntityType = nameof(InternshipApplication),
                    EntityId = oldAppId,
                    PerformedById = currentUserId,
                    Metadata = $"{{\"studentId\":\"{studentId}\",\"oldEnterpriseId\":\"{oldAppEnterpriseId}\",\"oldEnterpriseName\":\"{previousPlacedEnterpriseName}\",\"note\":\"withdrawn due to reassign from placed\"}}"
                });

                // 2) Reset StudentTerm placement status -> Unplaced and EnterpriseId cleared for that term
                var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
                    .FirstOrDefaultAsync(st => st.StudentId == studentId && st.TermId == app.TermId && st.DeletedAt == null, cancellationToken);

                if (studentTerm != null)
                {
                    var oldPlacement = studentTerm.PlacementStatus;
                    var oldTermEnt = studentTerm.EnterpriseId;

                    studentTerm.PlacementStatus = PlacementStatus.Unplaced;
                    studentTerm.EnterpriseId = null;
                    await _unitOfWork.Repository<StudentTerm>().UpdateAsync(studentTerm, cancellationToken);

                    // Audit StudentTerm change
                    auditLogs.Add(new AuditLog
                    {
                        AuditLogId = Guid.NewGuid(),
                        Action = AuditAction.Unassign,
                        EntityType = nameof(StudentTerm),
                        EntityId = studentTerm.StudentTermId,
                        PerformedById = currentUserId,
                        Metadata = $"{{\"studentId\":\"{studentTerm.StudentId}\",\"termId\":\"{studentTerm.TermId}\",\"oldPlacement\":\"{oldPlacement}\",\"note\":\"reset due to reassign\"}}"
                    });
                }

                // 3) Update student internship status
                var student = await _unitOfWork.Repository<Student>().Query()
                    .FirstOrDefaultAsync(s => s.StudentId == studentId, cancellationToken);

                if (student != null)
                {
                    var oldStudentStatus = student.InternshipStatus;
                    student.InternshipStatus = StudentStatus.Unplaced;
                    await _unitOfWork.Repository<Student>().UpdateAsync(student, cancellationToken);

                    // Audit student status change
                    auditLogs.Add(new AuditLog
                    {
                        AuditLogId = Guid.NewGuid(),
                        Action = AuditAction.Unassign,
                        EntityType = nameof(Student),
                        EntityId = student.StudentId,
                        PerformedById = currentUserId,
                        Metadata = $"{{\"studentId\":\"{student.StudentId}\",\"oldStatus\":\"{oldStudentStatus}\",\"newStatus\":\"{StudentStatus.Unplaced}\",\"note\":\"status reset due to reassign\"}}"
                    });
                }

                // 4) Create new PendingAssignment application for new enterprise
                var newApp = new InternshipApplication
                {
                    ApplicationId = Guid.NewGuid(),
                    EnterpriseId = request.NewEnterpriseId,
                    TermId = app.TermId,
                    StudentId = studentId,
                    JobId = null,
                    InternPhaseId = request.NewInternPhaseId, // Cần thêm vào command,
                    Status = InternshipApplicationStatus.PendingAssignment,
                    Source = ApplicationSource.UniAssign,
                    UniversityId = universityUser.UniversityId,
                    AppliedAt = DateTime.UtcNow,
                    CreatedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<InternshipApplication>().AddAsync(newApp, cancellationToken);

                // Audit creation of the new pending application (Assign)
                auditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Assign,
                    EntityType = nameof(InternshipApplication),
                    EntityId = newApp.ApplicationId,
                    PerformedById = currentUserId,
                    Metadata = $"{{\"studentId\":\"{studentId}\",\"newEnterpriseId\":\"{request.NewEnterpriseId}\",\"newInternPhaseId\":\"{request.NewInternPhaseId}\",\"note\":\"created pending due to reassign from placed\"}}"
                });

                // Persist everything including audit logs
                if (auditLogs.Any())
                {
                    await _unitOfWork.Repository<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // 5) After persist: notify student (AC-07) about reassign from Placed
                if (app.Student?.UserId != Guid.Empty)
                {
                    var newEnterpriseName = await _unitOfWork.Repository<Enterprise>().Query().Where(e => e.EnterpriseId == request.NewEnterpriseId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
                    await _publisher.Publish(new ApplicationReassignedFromPlacedEvent(
                        app.Student!.UserId,
                        newApp.ApplicationId,
                        oldEnterpriseName,
                        newEnterpriseName), cancellationToken);
                }

                // 6) Response + optional notification (UI shows toast)
                var resp = new ReAssignSingleResponse
                {
                    OldApplicationId = app.ApplicationId,
                    NewApplicationId = newApp.ApplicationId,
                    Status = newApp.Status,
                    StatusLabel = newApp.Status.ToString(),
                    UpdatedAt = DateTime.UtcNow,
                    Message = $"Đã gửi chỉ định {(await _unitOfWork.Repository<Enterprise>().Query().Where(e => e.EnterpriseId == request.NewEnterpriseId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken))} cho {studentName}. Đang chờ doanh nghiệp xác nhận."
                };

                _logger.LogInformation("ReAssignSingle completed: oldApp {OldApp} -> newApp {NewApp}", resp.OldApplicationId, resp.NewApplicationId);

                return Result<ReAssignSingleResponse>.Success(resp, resp.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning application for StudentId {StudentId}", request.StudentId);
                return Result<ReAssignSingleResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}

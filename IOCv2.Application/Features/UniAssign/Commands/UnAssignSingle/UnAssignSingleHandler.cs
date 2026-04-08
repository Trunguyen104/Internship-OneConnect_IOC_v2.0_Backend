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

namespace IOCv2.Application.Features.UniAssign.Commands.UnAssignSingle
{
    public class UnAssignSingleHandler : IRequestHandler<UnAssignSingleCommand, Result<UnAssignSingleResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IPublisher _publisher;
        private readonly ILogger<UnAssignSingleHandler> _logger;

        public UnAssignSingleHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IPublisher publisher,
            ILogger<UnAssignSingleHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task<Result<UnAssignSingleResponse>> Handle(UnAssignSingleCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("UnAssignSingle requested for ApplicationId {StudentId}", request.StudentId);

            try
            {
                if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                    return Result<UnAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                        ResultErrorType.Unauthorized);

                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query().AsNoTracking()
                    .FirstOrDefaultAsync(uu => uu.UserId == currentUserId, cancellationToken);

                if (universityUser == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.UniAdminInternship.LogUniversityUserNotFound), currentUserId);
                    return Result<UnAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.UniAdminInternship.UniversityUserNotFound),
                        ResultErrorType.Forbidden);
                }

                var app = await _unitOfWork.Repository<InternshipApplication>().Query()
                    .Include(a => a.Student).ThenInclude(s => s.User)
                    .Include(a => a.Enterprise)
                    .Include(a => a.Term)
                    .Include(a => a.Job)
                    .FirstOrDefaultAsync(a => a.StudentId == request.StudentId && UniAssignParam.CommonUniAssignParam.AllowedStatuses.Contains(a.Status), cancellationToken);

                if (app == null)
                    return Result<UnAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipApplication.NotFound),
                        ResultErrorType.NotFound);

                if (app.Source != ApplicationSource.UniAssign)
                    return Result<UnAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.HRApplications.NotUniAssignApplication),
                        ResultErrorType.BadRequest);

                if (app.Status != InternshipApplicationStatus.PendingAssignment &&
                    app.Status != InternshipApplicationStatus.Placed)
                {
                    return Result<UnAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.HRApplications.ApplicationNotPlaced),
                        ResultErrorType.BadRequest);
                }

                var term = await _unitOfWork.Repository<Term>().Query().AsNoTracking()
                    .FirstOrDefaultAsync(t => t.TermId == app.TermId, cancellationToken);

                if (term != null && term.Status == TermStatus.Closed)
                {
                    return Result<UnAssignSingleResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.TermEndedOrClosed),
                        ResultErrorType.BadRequest);
                }

                var studentId = app.StudentId;
                var studentName = app.Student?.User?.FullName ?? string.Empty;
                var enterpriseName = app.Enterprise?.Name ?? string.Empty;
                var enterpriseId = app.EnterpriseId;

                // Collect internship group ids under this enterprise for the student (to check logbook/eval/sprint)
                var internshipIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .Include(isv => isv.InternshipGroup)
                    .Where(isv => isv.StudentId == studentId
                                  && isv.InternshipGroup.EnterpriseId == enterpriseId
                                  && isv.DeletedAt == null)
                    .Select(isv => isv.InternshipId)
                    .ToListAsync(cancellationToken);

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

                    // Sprint detection (sprints are linked via Project -> InternshipId)
                    var projectInternshipIds = await _unitOfWork.Repository<Project>().Query()
                        .AsNoTracking()
                        .Where(p => p.InternshipId.HasValue && internshipIds.Contains(p.InternshipId!.Value))
                        .Select(p => p.ProjectId)
                        .ToListAsync(cancellationToken);

                    if (projectInternshipIds.Any())
                    {
                        hasSprints = await _unitOfWork.Repository<Sprint>().Query()
                            .AsNoTracking()
                            .AnyAsync(s => projectInternshipIds.Contains(s.ProjectId), cancellationToken);
                    }
                }

                if (hasLogbooks || hasSprints || hasEvaluations)
                {
                    var blockMsg = _messageService.GetMessage(MessageKeys.UniAssign.StudentHasPriorInternshipData);
                    _logger.LogInformation("Hard-block unassign for student {StudentId} due to existing internship data.", studentId);
                    return Result<UnAssignSingleResponse>.Failure(blockMsg, ResultErrorType.Conflict);
                }

                // Proceed with unassign
                var previousStatus = app.Status;
                app.Status = InternshipApplicationStatus.Withdrawn;
                app.UpdatedAt = DateTime.UtcNow;

                var history = new ApplicationStatusHistory
                {
                    HistoryId = Guid.NewGuid(),
                    ApplicationId = app.ApplicationId,
                    FromStatus = previousStatus,
                    ToStatus = app.Status,
                    Note = "UniAdmin unassign single (AC-06)",
                    TriggerSource = "Uni",
                    ChangedByName = universityUser.UserId.ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(history, cancellationToken);

                // Prepare audit logs
                var auditLogs = new List<AuditLog>();

                // Update StudentTerm placement status and enterprise link
                var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
                    .FirstOrDefaultAsync(st => st.StudentId == studentId && st.TermId == app.TermId && st.DeletedAt == null, cancellationToken);

                if (app.Status == InternshipApplicationStatus.Withdrawn && app.Source == ApplicationSource.UniAssign)
                {
                    // For PendingAssignment: placement_status remains Unplaced (already likely Unplaced).
                    // For Placed: AC-06 requires placement_status -> Unplaced and enterprise removed.
                    if (previousStatus == InternshipApplicationStatus.Placed)
                    {
                        if (studentTerm != null)
                        {
                            var oldPlacement = studentTerm.PlacementStatus;
                            var oldTermEnt = studentTerm.EnterpriseId;

                            studentTerm.PlacementStatus = PlacementStatus.Unplaced;
                            studentTerm.EnterpriseId = null;
                            studentTerm.UpdatedAt = DateTime.UtcNow;
                            studentTerm.UpdatedBy = currentUserId;
                            await _unitOfWork.Repository<StudentTerm>().UpdateAsync(studentTerm, cancellationToken);

                            // audit studentTerm change
                            auditLogs.Add(new AuditLog
                            {
                                AuditLogId = Guid.NewGuid(),
                                Action = AuditAction.Unassign,
                                EntityType = nameof(StudentTerm),
                                EntityId = studentTerm.StudentTermId,
                                PerformedById = currentUserId,
                                Metadata = $"{{\"studentId\":\"{studentTerm.StudentId}\",\"termId\":\"{studentTerm.TermId}\",\"oldPlacement\":\"{oldPlacement}\",\"note\":\"reset due to unassign\"}}"
                            });
                        }

                        var student = await _unitOfWork.Repository<Student>().Query()
                            .FirstOrDefaultAsync(s => s.StudentId == studentId, cancellationToken);

                        if (student != null)
                        {
                            var oldStudentStatus = student.InternshipStatus;
                            student.InternshipStatus = StudentStatus.Unplaced;
                            await _unitOfWork.Repository<Student>().UpdateAsync(student, cancellationToken);

                            // audit student status change
                            auditLogs.Add(new AuditLog
                            {
                                AuditLogId = Guid.NewGuid(),
                                Action = AuditAction.Unassign,
                                EntityType = nameof(Student),
                                EntityId = student.StudentId,
                                PerformedById = currentUserId,
                                Metadata = $"{{\"studentId\":\"{student.StudentId}\",\"oldStatus\":\"{oldStudentStatus}\",\"newStatus\":\"{StudentStatus.Unplaced}\",\"note\":\"status reset due to unassign\"}}"
                            });
                        }

                        // Notify Enterprise HR + student/uni admin (reuse existing event)
                        await _publisher.Publish(new PlacedStudentRemovedEvent(
                            StudentUserId: student?.UserId ?? Guid.Empty,
                            ApplicationId: app.ApplicationId,
                            EnterpriseName: enterpriseName,
                            UniversityId: app.UniversityId,
                            StudentName: studentName), cancellationToken);
                    }
                    else // PendingAssignment
                    {
                        // studentTerm.PlacementStatus remains Unplaced (no change needed). Just notify enterprise HR.
                        await _publisher.Publish(new ApplicationAutoWithdrawnNotifyEnterpriseEvent(
                            EnterpriseId: enterpriseId,
                            StudentName: studentName), cancellationToken);
                    }

                    // AC-07: notify the student in-app about the unassign (both Pending and Placed)
                    if (app.Student?.UserId != Guid.Empty)
                    {
                        var termName = app.Term?.Name ?? string.Empty;
                        await _publisher.Publish(new ApplicationUnassignedUniAssignEvent(
                            app.Student!.UserId,
                            app.ApplicationId,
                            termName), cancellationToken);
                    }
                }

                // audit application withdrawal (unassign)
                auditLogs.Add(new AuditLog
                {
                    AuditLogId = Guid.NewGuid(),
                    Action = AuditAction.Unassign,
                    EntityType = nameof(InternshipApplication),
                    EntityId = app.ApplicationId,
                    PerformedById = currentUserId,
                    Metadata = $"{{\"studentId\":\"{app.StudentId}\",\"applicationId\":\"{app.ApplicationId}\",\"oldStatus\":\"{previousStatus}\",\"oldEnterpriseId\":\"{enterpriseId}\",\"oldEnterpriseName\":\"{enterpriseName}\",\"note\":\"withdrawn by university unassign\"}}"
                });

                await _unitOfWork.Repository<InternshipApplication>().UpdateAsync(app, cancellationToken);

                // persist audit logs together with domain changes
                if (auditLogs.Any())
                {
                    await _unitOfWork.Repository<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);

                var resp = new UnAssignSingleResponse
                {
                    ApplicationId = app.ApplicationId,
                    Status = app.Status,
                    StatusLabel = app.Status.ToString(),
                    UpdatedAt = DateTime.UtcNow,
                    Message = _messageService.GetMessage(MessageKeys.UniAssign.UnassignSuccess, studentName)
                };

                return Result<UnAssignSingleResponse>.Success(resp, resp.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unassigning application {StudentId}", request.StudentId);
                return Result<UnAssignSingleResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}

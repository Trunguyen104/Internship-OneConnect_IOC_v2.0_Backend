using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.UniAssigns;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkUnassign
{
    internal class BulkUnassignHandler : IRequestHandler<BulkUnassignCommand, Result<BulkUnassignResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<BulkUnassignHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IPublisher _publisher;

        public BulkUnassignHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<BulkUnassignHandler> logger, IMessageService messageService, IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
            _messageService = messageService;
            _publisher = publisher;
        }

        public async Task<Result<BulkUnassignResponse>> Handle(BulkUnassignCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = Guid.Parse(_currentUser.UserId!);
            if (UniAssignParam.CreateUniAssignParam.UniAllowedRole.Contains(_currentUser.Role))
            {
                var currentUserUniversityId = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .Where(x => x.UserId == currentUserId)
                    .Select(x => x.UniversityId)
                    .FirstOrDefaultAsync(cancellationToken);

                // Build requested student id list: prefer `StudentIds` list from request
                var requestedStudentIds = (request.StudentIds != null && request.StudentIds.Any())
                    ? request.StudentIds.Distinct().ToList()
                    : new List<Guid>();

                // Load students matching requested ids
                var students = await _unitOfWork.Repository<Student>()
                    .Query()
                    .Where(s => requestedStudentIds.Contains(s.StudentId))
                    .Select(s => new { s.StudentId, s.UserId, s.InternshipStatus, s.User.FullName })
                    .ToListAsync(cancellationToken);

                // Check for missing students
                if (students.Count != requestedStudentIds.Count)
                {
                    var missing = requestedStudentIds.Except(students.Select(s => s.StudentId));
                    return Result<BulkUnassignResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.StudentsNotFound, string.Join(", ", missing)), ResultErrorType.NotFound);
                }

                // Verify each student's university matches current user's university
                var studentUserIds = students.Select(s => s.UserId).Distinct().ToList();
                var studentUniversities = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .Where(u => studentUserIds.Contains(u.UserId))
                    .Select(u => new { u.UserId, u.UniversityId })
                    .ToListAsync(cancellationToken);

                var unauthorized = students
                    .Where(s => studentUniversities.FirstOrDefault(u => u.UserId == s.UserId)?.UniversityId != currentUserUniversityId)
                    .Select(s => s.StudentId)
                    .ToList();

                if (unauthorized.Any())
                {
                    return Result<BulkUnassignResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.StudentsUnauthorized, string.Join(", ", unauthorized)), ResultErrorType.Unauthorized);
                }

                // Check term status for the applications involved: cannot unassign from closed terms
                var terms = await _unitOfWork.Repository<InternshipApplication>().Query()
                    .Where(ia => requestedStudentIds.Contains(ia.StudentId))
                    .Select(ia => ia.Term)
                    .ToListAsync(cancellationToken);

                if (terms.Any(t => t.Status == TermStatus.Closed))
                {
                    return Result<BulkUnassignResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.CannotUnassignFromClosedTerms), ResultErrorType.BadRequest);
                }

                var studentIds = students.Select(s => s.StudentId).Distinct().ToList();

                // Query for students that already have any related records and exclude them
                var hasLogbookIds = await _unitOfWork.Repository<Logbook>()
                    .Query()
                    .Where(lb => lb.StudentId.HasValue && studentIds.Contains(lb.StudentId.Value))
                    .Select(lb => lb.StudentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Sửa lại query Sprint để lấy StudentId, không phải SprintId
                var hasSprintStudentIds = await _unitOfWork.Repository<InternshipStudent>()
                    .Query()
                    .Where(x => studentIds.Contains(x.StudentId))
                    .Select(x => x.StudentId)  // ← Lấy StudentId trực tiếp
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var hasEvaluationIds = await _unitOfWork.Repository<Evaluation>()
                    .Query()
                    .Where(ev => ev.StudentId.HasValue && studentIds.Contains(ev.StudentId.Value))
                    .Select(ev => ev.StudentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var excludedIds = new HashSet<Guid>(hasLogbookIds.Where(id => id.HasValue).Select(id => id!.Value));
                foreach (var id in hasSprintStudentIds) excludedIds.Add(id);
                foreach (var id in hasEvaluationIds.Where(id => id.HasValue).Select(id => id!.Value)) excludedIds.Add(id);

                if (excludedIds.Any())
                {
                    students = students.Where(s => !excludedIds.Contains(s.StudentId)).ToList();
                    requestedStudentIds = requestedStudentIds.Except(excludedIds).ToList();

                    _logger.LogInformation("Excluded {Count} students from unassignment due to existing logbook/sprint/evaluation: {Ids}", excludedIds.Count, string.Join(", ", excludedIds));

                    if (!students.Any())
                    {
                        var message = _messageService.GetMessage(MessageKeys.UniAssign.PendingStudentsWithExistingData, string.Join(", ", excludedIds));
                        var response = new BulkUnassignResponse
                        {
                            Message = message
                        };

                        return Result<BulkUnassignResponse>.Success(response, message);
                    }
                }

                // Load related applications for remaining students (only UniAssign source)
                var remainingStudentIds = students.Select(s => s.StudentId).Distinct().ToList();
                var apps = await _unitOfWork.Repository<InternshipApplication>()
                    .Query()
                    .Include(a => a.Student).ThenInclude(s => s.User)
                    .Include(a => a.Job)
                    .Where(a => remainingStudentIds.Contains(a.StudentId) && a.Source == ApplicationSource.UniAssign && (a.Status == InternshipApplicationStatus.PendingAssignment || a.Status == InternshipApplicationStatus.Placed))
                    .ToListAsync(cancellationToken);

                if (!apps.Any())
                {
                    return Result<BulkUnassignResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.NoUniAssignApplicationsFound), ResultErrorType.NotFound);
                }

                var now = DateTime.UtcNow;
                var processedStudentIds = new List<Guid>();

                // collect audit logs to persist in the same transaction
                var auditLogs = new List<AuditLog>();

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    foreach (var app in apps)
                    {
                        var studentName = app.Student?.User?.FullName ?? string.Empty;
                        var jobTitle = app.Job?.Title ?? "(chưa rõ vị trí)";

                        // Record history entry
                        await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(new ApplicationStatusHistory
                        {
                            HistoryId = Guid.NewGuid(),
                            ApplicationId = app.ApplicationId,
                            FromStatus = app.Status,
                            ToStatus = InternshipApplicationStatus.Withdrawn,
                            TriggerSource = "University-Unassign",
                            ChangedByName = _currentUser.UserId,
                            Note = "University bulk unassign action",
                            CreatedAt = now
                        }, cancellationToken);

                        // capture old values for audit metadata
                        var oldStatus = app.Status;
                        var oldEnterpriseId = app.EnterpriseId;
                        var oldEnterpriseName = app.Enterprise?.Name ?? string.Empty;

                        // If the application was Placed -> also reset placement on StudentTerm + Student
                        if (app.Status == InternshipApplicationStatus.Placed)
                        {
                            // Update student overall status
                            if (app.Student != null)
                            {
                                var oldStudentStatus = app.Student.InternshipStatus;
                                app.Student.InternshipStatus = StudentStatus.Unplaced;
                                await _unitOfWork.Repository<Student>().UpdateAsync(app.Student, cancellationToken);

                                // audit student status change
                                auditLogs.Add(new AuditLog
                                {
                                    AuditLogId = Guid.NewGuid(),
                                    Action = AuditAction.Unassign,
                                    EntityType = nameof(Student),
                                    EntityId = app.Student.StudentId,
                                    PerformedById = currentUserId,
                                    Metadata = $"{{\"studentId\":\"{app.Student.StudentId}\",\"oldStatus\":\"{oldStudentStatus}\",\"newStatus\":\"{StudentStatus.Unplaced}\"}}"
                                });
                            }

                            // Reset StudentTerm placement (if exists)
                            var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
                                .FirstOrDefaultAsync(st => st.StudentId == app.StudentId && st.TermId == app.TermId, cancellationToken);

                            if (studentTerm != null)
                            {
                                var oldPlacement = studentTerm.PlacementStatus;
                                var oldTermEnt = studentTerm.EnterpriseId;
                                studentTerm.PlacementStatus = PlacementStatus.Unplaced;
                                studentTerm.EnterpriseId = null;
                                studentTerm.UpdatedAt = now;
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

                            // Note: capacity is computed live (phase capacity vs placed count). If you have an explicit remaining slot field on Job/Phase, update it here.
                        }

                        // Withdraw the application
                        app.Status = InternshipApplicationStatus.Withdrawn;
                        app.UpdatedBy = currentUserId;
                        app.UpdatedAt = now;
                        await _unitOfWork.Repository<InternshipApplication>().UpdateAsync(app, cancellationToken);

                        // audit application withdrawal (unassign)
                        auditLogs.Add(new AuditLog
                        {
                            AuditLogId = Guid.NewGuid(),
                            Action = AuditAction.Unassign,
                            EntityType = nameof(InternshipApplication),
                            EntityId = app.ApplicationId,
                            PerformedById = currentUserId,
                            Metadata = $"{{\"studentId\":\"{app.StudentId}\",\"applicationId\":\"{app.ApplicationId}\",\"oldStatus\":\"{oldStatus}\",\"oldEnterpriseId\":\"{oldEnterpriseId}\",\"oldEnterpriseName\":\"{oldEnterpriseName}\",\"note\":\"withdrawn by university bulk unassign\"}}"
                        });

                        // Persist per-application changes now (will be saved with SaveChangeAsync below inside transaction)
                        processedStudentIds.Add(app.StudentId);

                        // Notify Enterprise HR: publish event intended to notify enterprise HR about auto-withdraw
                        if (app.EnterpriseId != Guid.Empty)
                        {
                            await _publisher.Publish(new ApplicationWithdrawnByStudentEvent(
                                app.EnterpriseId,
                                studentName,
                                jobTitle,
                                app.ApplicationId), cancellationToken);

                            // Also publish an auto-withdraw enterprise-specific event if any handlers expect it
                            await _publisher.Publish(new ApplicationAutoWithdrawnNotifyEnterpriseEvent(
                                app.EnterpriseId,
                                studentName), cancellationToken);
                        }

                        // AC-07: notify the student in-app about the unassign
                        if (app.Student != null && app.Student.UserId != Guid.Empty)
                        {
                            var termName = app.Term?.Name ?? string.Empty;
                            await _publisher.Publish(new ApplicationUnassignedUniAssignEvent(
                                app.Student.UserId,
                                app.ApplicationId,
                                termName), cancellationToken);
                        }

                        // If was Placed: notify student + uni admins (reuse PlacedStudentRemovedEvent used for HR removal scenarios)
                        if (app.Status == InternshipApplicationStatus.Withdrawn && app.Student != null && app.UniversityId.HasValue)
                        {
                            await _publisher.Publish(new PlacedStudentRemovedEvent(
                                StudentUserId: app.Student.UserId,
                                ApplicationId: app.ApplicationId,
                                EnterpriseName: app.Enterprise?.Name ?? string.Empty,
                                UniversityId: app.UniversityId,
                                StudentName: studentName), cancellationToken);
                        }
                    }

                    // persist audit logs within same transaction
                    if (auditLogs.Any())
                    {
                        await _unitOfWork.Repository<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
                    }

                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    var toast = _messageService.GetMessage(MessageKeys.UniAssign.BulkUnassignSuccess, processedStudentIds.Count);
                    _logger.LogInformation("Bulk unassign processed {Count} students by university user {UserId}", processedStudentIds.Count, currentUserId);

                    var response = new BulkUnassignResponse
                    {
                        Message = toast
                    };

                    return Result<BulkUnassignResponse>.Success(response, toast);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during bulk unassign by university user {UserId}", currentUserId);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<BulkUnassignResponse>.Failure(ex.Message, ResultErrorType.InternalServerError);
                }
            }
            return Result<BulkUnassignResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
        }
    }
}
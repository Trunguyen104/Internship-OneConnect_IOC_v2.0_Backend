using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.UniAssigns;
using IOCv2.Application.Features.UniAssign.Commands.QuickEnterpriseAssignment;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
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
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkEnterpriseAssignment
{
    internal class BulkEnterpriseAssignmentHandler : IRequestHandler<BulkEnterpriseAssignmentCommand, Result<BulkEnterpriseAssignmentResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<BulkEnterpriseAssignmentHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly IPublisher _publisher;

        public BulkEnterpriseAssignmentHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<BulkEnterpriseAssignmentHandler> logger, IMessageService messageService, IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
            _messageService = messageService;
            _publisher = publisher;
        }

        public async Task<Result<BulkEnterpriseAssignmentResponse>> Handle(BulkEnterpriseAssignmentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = Guid.Parse(_currentUser.UserId!);
            if (UniAssignParam.CreateUniAssignParam.UniAllowedRole.Contains(_currentUser.Role))
            {
                var currentUserUniversityId = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .Where(x => x.UserId == currentUserId)
                    .Select(x => x.UniversityId)
                    .FirstOrDefaultAsync(cancellationToken);

                // Build requested student id list: prefer `StudentIds` list from request, otherwise use single `StudentId`.
                var requestedStudentIds = (request.StudentIds != null && request.StudentIds.Any())
                    ? request.StudentIds.Distinct().ToList()
                    : new List<Guid>();

                // Load students matching requested ids
                var students = await _unitOfWork.Repository<Student>()
                    .Query()
                    .Where(s => requestedStudentIds.Contains(s.StudentId))
                    .Select(s => new { s.StudentId, s.UserId, s.InternshipStatus })
                    .ToListAsync(cancellationToken);

                // Check for missing students
                if (students.Count != requestedStudentIds.Count)
                {
                    var missing = requestedStudentIds.Except(students.Select(s => s.StudentId));
                    var message = string.Format(_messageService.GetMessage(MessageKeys.UniAssign.StudentsNotFound), string.Join(", ", missing));
                    return Result<BulkEnterpriseAssignmentResponse>.Failure(message, ResultErrorType.NotFound);
                }

                // Force semantics: when Force == false we should block already placed students; when Force == true allow overriding placed students.
                if (!request.Force)
                {
                    // Check for already placed students
                    var placed = students.Where(s => s.InternshipStatus == StudentStatus.Placed).Select(s => s.StudentId).ToList();
                    if (placed.Any())
                    {
                        var message = string.Format(_messageService.GetMessage(MessageKeys.UniAssign.StudentsAlreadyPlaced), string.Join(", ", placed));
                        return Result<BulkEnterpriseAssignmentResponse>.Failure(message, ResultErrorType.BadRequest);
                    }
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
                    var message = string.Format(_messageService.GetMessage(MessageKeys.UniAssign.StudentsUnauthorized), string.Join(", ", unauthorized));
                    return Result<BulkEnterpriseAssignmentResponse>.Failure(message, ResultErrorType.Unauthorized);
                }

                var term = await (
                    from st in _unitOfWork.Repository<StudentTerm>().Query()
                    join t in _unitOfWork.Repository<Term>().Query() on st.TermId equals t.TermId
                    where requestedStudentIds.Contains(st.StudentId) && t.Status == TermStatus.Open
                    orderby t.StartDate descending
                    select t
                ).FirstOrDefaultAsync(cancellationToken);

                if (term == null) return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.NotFound);
                if (term.Status != TermStatus.Open) return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.TermNotOpenForAssignment), ResultErrorType.BadRequest);

                // Initial read of internship phase (non-transactional read to validate existence and metadata)
                var internshipPhase = await _unitOfWork.Repository<InternshipPhase>()
                    .Query()
                    .Where(x => x.PhaseId == request.InternPhaseId)
                    .Include(ia => ia.Jobs)
                        .ThenInclude(j => j.InternshipApplications)
                    .Include(i => i.Enterprise)
                    .Select(x => new
                    {
                        x.PhaseId,
                        x.EnterpriseId,
                        x.StartDate,
                        x.EndDate,
                        x.Capacity,
                        EnterpriseName = x.Enterprise!.Name,
                        x.Jobs,
                        PhaseName = x.Name
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (internshipPhase == null) return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.InternshipPhaseNotFound), ResultErrorType.NotFound);
                // Validate phase date ordering
                if (internshipPhase.StartDate > internshipPhase.EndDate)
                {
                    return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.InternshipPhaseStartDateAfterEndDate), ResultErrorType.BadRequest);
                }
                // Ensure the internship phase is within the term date range
                if (internshipPhase.StartDate < term.StartDate || internshipPhase.EndDate > term.EndDate)
                {
                    return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.InternshipPhaseNotWithinTermDates), ResultErrorType.BadRequest);
                }
                // Ensure the internship phase has at least one job posting with status Published or Closed.
                var hasJobPosting = await _unitOfWork.Repository<Job>().Query()
                    .Where(j => j.InternshipPhaseId == internshipPhase.PhaseId && (j.Status == JobStatus.PUBLISHED || j.Status == JobStatus.CLOSED))
                    .AnyAsync(cancellationToken);
                if (!hasJobPosting)
                {
                    return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.InternshipPhaseMustHaveAtLeastOnePublishedOrClosedJobPosting), ResultErrorType.BadRequest);
                }

                var selectedCount = requestedStudentIds.Count;

                // We'll use a retry loop and perform final capacity validation while inside a DB transaction.
                // NOTE: This mitigates race conditions by re-checking capacity inside the transaction and retrying on transient concurrency failures.
                const int maxAttempts = 3;
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        await _unitOfWork.BeginTransactionAsync(cancellationToken);

                        // Re-load internshipPhase within transaction and compute remaining capacity from DB state
                        var lockedPhase = await _unitOfWork.Repository<InternshipPhase>()
                            .Query()
                            .Where(x => x.PhaseId == internshipPhase.PhaseId)
                            .Include(ia => ia.Jobs)
                                .ThenInclude(j => j.InternshipApplications)
                            .Include(i => i.Enterprise)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (lockedPhase == null)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipPhase.NotFound), ResultErrorType.NotFound);
                        }

                        var remainingCapacity = lockedPhase.Capacity - lockedPhase.Jobs.SelectMany(j => j.InternshipApplications).Count(ia => ia.Status == InternshipApplicationStatus.Placed);
                        var availableSlots = Math.Max(0, remainingCapacity);

                        if (selectedCount > availableSlots)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            return Result<BulkEnterpriseAssignmentResponse>.Failure(
                                _messageService.GetMessage(MessageKeys.UniAssign.InternPhaseInsufficientSlots , internshipPhase.PhaseName, internshipPhase.EnterpriseName, availableSlots, selectedCount),
                                ResultErrorType.BadRequest);
                        }

                        var now = DateTime.UtcNow;

                        // Load existing uni-assign applications for these students in the same term that are PendingAssignment or Placed
                        var existingApps = await _unitOfWork.Repository<InternshipApplication>().Query()
                            .Include(a => a.Student).ThenInclude(s => s.User)
                            .Include(a => a.Enterprise)
                            .Where(a => requestedStudentIds.Contains(a.StudentId)
                                        && a.TermId == term.TermId
                                        && (a.Status == InternshipApplicationStatus.PendingAssignment || a.Status == InternshipApplicationStatus.Placed)
                                        && a.Source == ApplicationSource.UniAssign)
                            .ToListAsync(cancellationToken);

                        var applicationsToAdd = new List<InternshipApplication>();
                        var pendingUpdatedApps = new List<InternshipApplication>();
                        var placedReplacedOldApps = new List<InternshipApplication>(); // old placed apps that will be marked Rejected
                        var publishEvents = new List<Func<CancellationToken, Task>>(); // deferred publishers to run after commit

                        // Audit logs collector
                        var auditLogs = new List<AuditLog>();

                        foreach (var sid in requestedStudentIds)
                        {
                            var stu = students.First(s => s.StudentId == sid);
                            var existing = existingApps.FirstOrDefault(a => a.StudentId == sid);

                            if (existing == null)
                            {
                                // No existing uni-assign app -> create new pending application
                                var newApp = new InternshipApplication
                                {
                                    ApplicationId = Guid.NewGuid(),
                                    EnterpriseId = request.EnterpriseId,
                                    TermId = term.TermId,
                                    StudentId = sid,
                                    InternPhaseId = request.InternPhaseId,
                                    Status = InternshipApplicationStatus.PendingAssignment,
                                    Source = ApplicationSource.UniAssign,
                                    AppliedAt = DateTime.UtcNow
                                };
                                applicationsToAdd.Add(newApp);

                                // create audit log for assign
                                auditLogs.Add(new AuditLog
                                {
                                    AuditLogId = Guid.NewGuid(),
                                    Action = AuditAction.Assign,
                                    EntityType = nameof(InternshipApplication),
                                    EntityId = newApp.ApplicationId,
                                    PerformedById = currentUserId,
                                    Metadata = $"{{\"studentId\":\"{sid}\",\"enterpriseId\":\"{request.EnterpriseId}\",\"internPhaseId\":\"{request.InternPhaseId}\",\"termId\":\"{term.TermId}\",\"note\":\"bulk assign - created pending\"}}"
                                });

                                // prepare publish: Assigned
                                var studentUserId = stu.UserId;
                                var newAppId = newApp.ApplicationId;
                                var enterpriseName = internshipPhase.EnterpriseName;
                                var termName = term.Name;
                                publishEvents.Add(ct => _publisher.Publish(new ApplicationAssignedUniAssignEvent(studentUserId, newAppId, enterpriseName, termName), ct));
                            }
                            else if (existing.Status == InternshipApplicationStatus.PendingAssignment)
                            {
                                // Reassign from pending: update existing application to new enterprise/phase if changed
                                var wasDifferent = existing.EnterpriseId != request.EnterpriseId || existing.InternPhaseId != request.InternPhaseId;
                                existing.EnterpriseId = request.EnterpriseId;
                                existing.InternPhaseId = request.InternPhaseId;
                                existing.AppliedAt = DateTime.UtcNow;
                                existing.Source = ApplicationSource.UniAssign;
                                pendingUpdatedApps.Add(existing);

                                // audit for reassignment / assign from pending
                                auditLogs.Add(new AuditLog
                                {
                                    AuditLogId = Guid.NewGuid(),
                                    Action = AuditAction.Assign,
                                    EntityType = nameof(InternshipApplication),
                                    EntityId = existing.ApplicationId,
                                    PerformedById = currentUserId,
                                    Metadata = $"{{\"studentId\":\"{sid}\",\"enterpriseId\":\"{request.EnterpriseId}\",\"internPhaseId\":\"{request.InternPhaseId}\",\"termId\":\"{term.TermId}\",\"wasDifferent\":{wasDifferent.ToString().ToLower()}}}"
                                });

                                if (wasDifferent)
                                {
                                    // prepare publish: Reassigned from pending (use existing.ApplicationId, new enterprise name)
                                    var studentUserId = existing.Student?.UserId ?? stu.UserId;
                                    var appId = existing.ApplicationId;
                                    var newEnterpriseName = internshipPhase.EnterpriseName;
                                    publishEvents.Add(ct => _publisher.Publish(new ApplicationReassignedFromPendingEvent(studentUserId, appId, newEnterpriseName), ct));
                                }
                                else
                                {
                                    // If nothing changed, still publish Assigned to notify anyway.
                                    var studentUserId = existing.Student?.UserId ?? stu.UserId;
                                    var appId = existing.ApplicationId;
                                    var enterpriseName = internshipPhase.EnterpriseName;
                                    var termName = term.Name;
                                    publishEvents.Add(ct => _publisher.Publish(new ApplicationAssignedUniAssignEvent(studentUserId, appId, enterpriseName, termName), ct));
                                }
                            }
                            else if (existing.Status == InternshipApplicationStatus.Placed)
                            {
                                // New: hard-block reassign from placed when student already has internship data (e.g., logbooks) at the old enterprise.
                                // This is a safety check: do not allow removing placement if there is existing internship data.
                                var hasInternshipData = await _unitOfWork.Repository<Logbook>().Query()
                                    .Where(lb => lb.StudentId == sid && lb.Internship.EnterpriseId == existing.EnterpriseId)
                                    .AnyAsync(cancellationToken);

                                if (hasInternshipData)
                                {
                                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                                    return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.CannotChangeEnterpriseStudentHasInternshipData), ResultErrorType.BadRequest);
                                }

                                // Reassign from placed: mark old placed app as Rejected (remove placement) and create a new pending application
                                var oldEnterpriseName = existing.Enterprise?.Name ?? string.Empty;

                                // mark old as Rejected and reset student status
                                existing.Status = InternshipApplicationStatus.Rejected;
                                if (existing.Student != null)
                                {
                                    existing.Student.InternshipStatus = StudentStatus.Unplaced;
                                }
                                placedReplacedOldApps.Add(existing);

                                // audit for unassigning old placed application
                                auditLogs.Add(new AuditLog
                                {
                                    AuditLogId = Guid.NewGuid(),
                                    Action = AuditAction.Unassign,
                                    EntityType = nameof(InternshipApplication),
                                    EntityId = existing.ApplicationId,
                                    PerformedById = currentUserId,
                                    Metadata = $"{{\"studentId\":\"{sid}\",\"oldEnterprise\":\"{oldEnterpriseName}\",\"note\":\"removed placement - marked Rejected\"}}"
                                });

                                // create new pending application
                                var newApp = new InternshipApplication
                                {
                                    ApplicationId = Guid.NewGuid(),
                                    EnterpriseId = request.EnterpriseId,
                                    TermId = term.TermId,
                                    StudentId = sid,
                                    InternPhaseId = request.InternPhaseId,
                                    Status = InternshipApplicationStatus.PendingAssignment,
                                    Source = ApplicationSource.UniAssign,
                                    AppliedAt = DateTime.UtcNow
                                };
                                applicationsToAdd.Add(newApp);

                                // audit for assigning new pending application
                                auditLogs.Add(new AuditLog
                                {
                                    AuditLogId = Guid.NewGuid(),
                                    Action = AuditAction.Assign,
                                    EntityType = nameof(InternshipApplication),
                                    EntityId = newApp.ApplicationId,
                                    PerformedById = currentUserId,
                                    Metadata = $"{{\"studentId\":\"{sid}\",\"enterpriseId\":\"{request.EnterpriseId}\",\"internPhaseId\":\"{request.InternPhaseId}\",\"termId\":\"{term.TermId}\",\"note\":\"reassigned from placed\"}}"
                                });

                                // prepare publish: Reassigned from placed (provide old and new names)
                                var studentUserId = existing.Student?.UserId ?? stu.UserId;
                                var newEnterpriseName = internshipPhase.EnterpriseName;
                                var newAppId = newApp.ApplicationId;
                                publishEvents.Add(ct => _publisher.Publish(new ApplicationReassignedFromPlacedEvent(studentUserId, newAppId, oldEnterpriseName, newEnterpriseName), ct));
                            }
                        }

                        if (applicationsToAdd.Any())
                            await _unitOfWork.Repository<InternshipApplication>().AddRangeAsync(applicationsToAdd, cancellationToken);

                        foreach (var upd in pendingUpdatedApps)
                            await _unitOfWork.Repository<InternshipApplication>().UpdateAsync(upd, cancellationToken);

                        foreach (var old in placedReplacedOldApps)
                            await _unitOfWork.Repository<InternshipApplication>().UpdateAsync(old, cancellationToken);

                        // persist audit logs within the same transaction
                        if (auditLogs.Any())
                        {
                            await _unitOfWork.Repository<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
                        }

                        await _unitOfWork.SaveChangeAsync(cancellationToken);

                        var response = new BulkEnterpriseAssignmentResponse
                        {
                            InternPhaseId = internshipPhase.PhaseId,
                            TermId = term.TermId,
                            EnterpriseId = request.EnterpriseId,
                            StudentIds = requestedStudentIds
                        };

                        await _unitOfWork.CommitTransactionAsync(cancellationToken);

                        _logger.LogInformation("Bulk assigned/updated {Count} students to intern phase {PhaseId} by user {UserId}", requestedStudentIds.Count, internshipPhase.PhaseId, currentUserId);

                        // Publish notifications after successful commit. Isolate failures per publish.
                        foreach (var publish in publishEvents)
                        {
                            try
                            {
                                await publish(cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to publish uni-assign notification event during bulk assignment by user {UserId}", currentUserId);
                                // swallow to avoid breaking overall success
                            }
                        }

                        return Result<BulkEnterpriseAssignmentResponse>.Success(response);
                    }
                    catch (DbUpdateConcurrencyException ex)
                    {
                        // concurrency conflict — rollback and retry
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        _logger.LogWarning(ex, "Concurrency conflict when bulk assigning students to phase {PhaseId} attempt {Attempt}", internshipPhase.PhaseId, attempt);
                        if (attempt == maxAttempts)
                        {
                            _logger.LogError(ex, "Exceeded retry attempts for bulk assign to phase {PhaseId}", internshipPhase.PhaseId);
                            return Result<BulkEnterpriseAssignmentResponse>.Failure("Concurrency error while assigning students. Please retry.", ResultErrorType.Conflict);
                        }
                        // small backoff
                        await Task.Delay(100 * attempt, cancellationToken);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        _logger.LogError(ex, "Error occurred while bulk assigning students to intern phase {PhaseId} by user {UserId}", internshipPhase.PhaseId, currentUserId);
                        return Result<BulkEnterpriseAssignmentResponse>.Failure(ex.Message, ResultErrorType.InternalServerError);
                    }
                }

                // If we fall through, treat as failure to assign after retries.
                return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.FailedToAssignStudentsDueToConcurrentOperations), ResultErrorType.Conflict);
            }

            return Result<BulkEnterpriseAssignmentResponse>.Failure(_messageService.GetMessage(MessageKeys.UniAssign.UnauthorizedOrInvalidOperation), ResultErrorType.Unauthorized);
        }
    }
}
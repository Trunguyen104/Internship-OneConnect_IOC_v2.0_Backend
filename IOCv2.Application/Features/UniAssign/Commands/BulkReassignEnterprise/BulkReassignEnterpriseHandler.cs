using DocumentFormat.OpenXml.InkML;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.UniAssigns;
using IOCv2.Application.Features.UniAssign.Commands.BulkEnterpriseAssignment;
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

namespace IOCv2.Application.Features.UniAssign.Commands.BulkAssign
{
    internal class BulkReassignEnterpriseHandler : IRequestHandler<BulkReassignEnterpriseCommand, Result<BulkReassignEnterpriseResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<BulkReassignEnterpriseHandler> _logger;
        private readonly IPublisher _publisher;


        public BulkReassignEnterpriseHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<BulkReassignEnterpriseHandler> logger, IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
            _publisher = publisher;
        }

        public async Task<Result<BulkReassignEnterpriseResponse>> Handle(BulkReassignEnterpriseCommand request, CancellationToken cancellationToken)
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
                    return Result<BulkReassignEnterpriseResponse>.Failure($"Students not found: {string.Join(", ", missing)}", ResultErrorType.NotFound);
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
                    return Result<BulkReassignEnterpriseResponse>.Failure($"You are not allowed to assign these students: {string.Join(", ", unauthorized)}", ResultErrorType.Unauthorized);
                }

                // --- New behavior: exclude students who already have logbook / sprint / evaluation ---
                var studentIds = students.Select(s => s.StudentId).Distinct().ToList();

                // IMPORTANT: fetch pending applications for the original student set BEFORE applying exclusions.
                // This is needed for BR-07: if a student is PendingAssignment AND already has internship data from another term => hard block.
                var initialPendingStudentIds = await _unitOfWork.Repository<InternshipApplication>()
                    .Query()
                    .Where(ia => studentIds.Contains(ia.StudentId) && ia.Status == InternshipApplicationStatus.PendingAssignment)
                    .Select(ia => ia.StudentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Query for students that already have any related records.
                // Note: Repository/entity names assumed to be Logbook, Sprint, Evaluation. Adjust if different in the domain.
                var hasLogbookIds = await _unitOfWork.Repository<Logbook>()
                    .Query()
                    .Where(lb => lb.StudentId.HasValue && studentIds.Contains(lb.StudentId.Value))
                    .Select(lb => lb.StudentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var hasSprintIds = await _unitOfWork.Repository<InternshipStudent>()
                    .Query()
                    .Where(x => studentIds.Contains(x.StudentId))
                    .SelectMany(x => x.InternshipGroup.Projects)
                    .SelectMany(p => p.Sprints)
                    .Select(x => x.SprintId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var hasEvaluationIds = await _unitOfWork.Repository<Evaluation>()
                    .Query()
                    .Where(ev => ev.StudentId.HasValue && studentIds.Contains(ev.StudentId.Value))
                    .Select(ev => ev.StudentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Convert nullable Guid lists to non-nullable for HashSet constructor
                var excludedIds = new HashSet<Guid>(hasLogbookIds.Where(id => id.HasValue).Select(id => id!.Value));
                foreach (var id in hasSprintIds) excludedIds.Add(id);
                foreach (var id in hasEvaluationIds.Where(id => id.HasValue).Select(id => id!.Value)) excludedIds.Add(id);

                // BR-07: If any student is both PendingAssignment and has internship data (logbook/sprint/evaluation),
                // this is not allowed — hard block the operation.
                var pendingWithExistingData = initialPendingStudentIds.Intersect(excludedIds).ToList();
                if (pendingWithExistingData.Any())
                {
                    var message = $"Không thể đổi enterprise cho những sinh viên sau vì họ đang ở trạng thái PendingAssignment nhưng đã có dữ liệu thực tập từ kỳ khác: {string.Join(", ", pendingWithExistingData)}";
                    _logger.LogWarning("Bulk reassignment blocked. Pending students with prior internship data: {Ids}", string.Join(", ", pendingWithExistingData));
                    return Result<BulkReassignEnterpriseResponse>.Failure(message, ResultErrorType.BadRequest);
                }

                if (excludedIds.Any())
                {
                    // Remove excluded students from working lists so they won't be reassigned.
                    students = students.Where(s => !excludedIds.Contains(s.StudentId)).ToList();
                    requestedStudentIds = requestedStudentIds.Except(excludedIds).ToList();

                    _logger.LogInformation("Excluded {Count} students from reassignment due to existing logbook/sprint/evaluation: {Ids}", excludedIds.Count, string.Join(", ", excludedIds));

                    // If all students are excluded, return a response indicating UI should disable confirm and show message.
                    if (!students.Any())
                    {
                        var message = "Không có sinh viên nào trong danh sách có thể đổi enterprise. Tất cả đã có dữ liệu thực tập.";
                        var response = new BulkReassignEnterpriseResponse
                        {
                            Message = message
                        };

                        // Return success result with the response and message so frontend can disable the confirm button and display the message.
                        return Result<BulkReassignEnterpriseResponse>.Success(response, message);
                    }
                }

                // --- Begin reassignment for the remaining students ---
                var remainingStudentIds = students.Select(s => s.StudentId).Distinct().ToList();
                if (!remainingStudentIds.Any())
                {
                    return Result<BulkReassignEnterpriseResponse>.Failure("No students available to reassign after exclusions.", ResultErrorType.BadRequest);
                }

                var term = await _unitOfWork.Repository<Term>().GetByIdAsync(request.TermId, cancellationToken);
                if (term == null) return Result<BulkReassignEnterpriseResponse>.Failure("Term not found.", ResultErrorType.NotFound);

                // Optional: if request exposes a TermStatus, check it and short-circuit if ended/closed.
                if (term.Status == TermStatus.Closed)
                {
                    // UI: should disable the button; return a failure that the UI can map to a disabled tooltip.
                    return Result<BulkReassignEnterpriseResponse>.Failure("Không thể thay đổi placement khi kỳ đã kết thúc.", ResultErrorType.BadRequest);
                }

                var now = DateTime.UtcNow;

                // 1) Find existing placed applications for these students to withdraw them
                var existingPlacedApps = await _unitOfWork.Repository<InternshipApplication>()
                    .Query()
                    .Where(ia => remainingStudentIds.Contains(ia.StudentId) && ia.Status == InternshipApplicationStatus.Placed)
                    .ToListAsync(cancellationToken);

                // Also fetch any existing pending applications for the same students (used to decide Assigned vs ReassignedFromPending)
                var existingPendingApps = await _unitOfWork.Repository<InternshipApplication>()
                    .Query()
                    .Where(ia => remainingStudentIds.Contains(ia.StudentId) && ia.Status == InternshipApplicationStatus.PendingAssignment)
                    .ToListAsync(cancellationToken);

                // --- New behavior: exclude students who already have an active self-apply at the target enterprise ---
                // Consider self-apply active statuses: Applied, Interviewing, Offered
                var activeSelfApplyStatuses = new[]
                {
                    InternshipApplicationStatus.Applied,
                    InternshipApplicationStatus.Interviewing,
                    InternshipApplicationStatus.Offered
                };

                var selfApplyAtTarget = await _unitOfWork.Repository<InternshipApplication>()
                    .Query()
                    .Where(a => remainingStudentIds.Contains(a.StudentId)
                                && a.EnterpriseId == request.NewEnterpriseId
                                && activeSelfApplyStatuses.Contains(a.Status))
                    .Select(a => a.StudentId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (selfApplyAtTarget.Any())
                {
                    // Exclude those students from reassignment
                    var selfApplyExcluded = new HashSet<Guid>(selfApplyAtTarget);
                    students = students.Where(s => !selfApplyExcluded.Contains(s.StudentId)).ToList();
                    remainingStudentIds = remainingStudentIds.Except(selfApplyExcluded).ToList();
                    requestedStudentIds = requestedStudentIds.Except(selfApplyExcluded).ToList();

                    _logger.LogInformation("Excluded {Count} students from reassignment because they have active self-apply at target enterprise {EnterpriseId}: {Ids}",
                        selfApplyExcluded.Count, request.NewEnterpriseId, string.Join(", ", selfApplyExcluded));

                    // If all students excluded, return response so UI can disable confirm and show message.
                    if (!students.Any())
                    {
                        var message = "Không có sinh viên nào có thể đổi enterprise vì một số sinh viên đang có đơn tự ứng tuyển đang xử lý tại doanh nghiệp đích.";
                        var response = new BulkReassignEnterpriseResponse
                        {
                            Message = message
                        };
                        return Result<BulkReassignEnterpriseResponse>.Success(response, message);
                    }
                }

                var studentTerm = await _unitOfWork.Repository<StudentTerm>()
                    .Query()
                    .Where(st => st.TermId == request.TermId && remainingStudentIds.Contains(st.StudentId))
                    .ToListAsync(cancellationToken);

                // Prepare audit logs collector
                var auditLogs = new List<AuditLog>();

                // Update StudentTerm placement status to Unplaced and set audit fields
                if (studentTerm.Any())
                {
                    foreach (var st in studentTerm)
                    {
                        st.PlacementStatus = PlacementStatus.Unplaced;
                        st.UpdatedBy = currentUserId;
                        st.UpdatedAt = now;

                        // Optional: record StudentTerm change as Unassign in audit log (if desired)
                        auditLogs.Add(new AuditLog
                        {
                            AuditLogId = Guid.NewGuid(),
                            Action = AuditAction.Unassign,
                            EntityType = nameof(StudentTerm),
                            EntityId = st.StudentTermId,
                            PerformedById = currentUserId,
                            Metadata = $"{{\"studentId\":\"{st.StudentId}\",\"termId\":\"{st.TermId}\",\"note\":\"placement status set to UnPlaced\"}}"
                        });
                    }
                }

                foreach (var app in existingPlacedApps)
                {
                    // record audit for withdrawing placed application (unassign)
                    auditLogs.Add(new AuditLog
                    {
                        AuditLogId = Guid.NewGuid(),
                        Action = AuditAction.Unassign,
                        EntityType = nameof(InternshipApplication),
                        EntityId = app.ApplicationId,
                        PerformedById = currentUserId,
                        Metadata = $"{{\"studentId\":\"{app.StudentId}\",\"enterpriseId\":\"{app.EnterpriseId}\",\"termId\":\"{request.TermId}\",\"note\":\"withdrawn due to reassignment\"}}"
                    });

                    app.Status = InternshipApplicationStatus.Withdrawn;
                    app.UpdatedBy = currentUserId;
                    app.UpdatedAt = now;
                }

                // Validate target intern phase id
                if (request.NewInternPhaseId == Guid.Empty)
                {
                    return Result<BulkReassignEnterpriseResponse>.Failure("Internship phase not found.", ResultErrorType.NotFound);
                }

                // --- Capacity check: ensure target intern phase has remaining capacity > 0 before creating pending applications ---
                // Compute capacity and placed count for the target phase (remainingCapacity = Capacity - placedCount)
                var phaseCapacity = await _unitOfWork.Repository<InternshipPhase>()
                    .Query()
                    .Where(p => p.PhaseId == request.NewInternPhaseId && p.DeletedAt == null)
                    .Select(p => p.Capacity)
                    .FirstOrDefaultAsync(cancellationToken);

                // If phase is not found
                if (phaseCapacity == 0 && !await _unitOfWork.Repository<InternshipPhase>().Query().AnyAsync(p => p.PhaseId == request.NewInternPhaseId && p.DeletedAt == null, cancellationToken))
                {
                    return Result<BulkReassignEnterpriseResponse>.Failure("Selected intern phase not found.", ResultErrorType.NotFound);
                }

                var placedInPhaseCount = await _unitOfWork.Repository<InternshipApplication>()
                    .Query()
                    .Where(a => a.InternPhaseId == request.NewInternPhaseId && a.Status == InternshipApplicationStatus.Placed)
                    .Select(a => a.StudentId)
                    .Distinct()
                    .CountAsync(cancellationToken);

                var remainingCapacity = Math.Max(phaseCapacity - placedInPhaseCount, 0);

                if (remainingCapacity <= 0)
                {
                    // No capacity in the chosen phase — abort and inform caller so UI can disable confirm
                    var message = "Không thể gửi chỉ định mới: intern phase đã hết chỗ (remainingCapacity = 0).";
                    _logger.LogInformation("Bulk reassignment aborted: target intern phase {PhaseId} has remainingCapacity {RemainingCapacity}", request.NewInternPhaseId, remainingCapacity);
                    var response = new BulkReassignEnterpriseResponse
                    {
                        Message = message
                    };
                    return Result<BulkReassignEnterpriseResponse>.Failure(message, ResultErrorType.BadRequest);
                }

                // 2) Create new pending applications at the target enterprise
                var newApplications = new List<InternshipApplication>();
                foreach (var sid in remainingStudentIds)
                {
                    var newApp = new InternshipApplication
                    {
                        ApplicationId = Guid.NewGuid(),
                        StudentId = sid,
                        EnterpriseId = request.NewEnterpriseId,
                        InternPhaseId = request.NewInternPhaseId,
                        TermId = request.TermId,
                        Status = InternshipApplicationStatus.PendingAssignment,
                        Source = ApplicationSource.UniAssign,
                        CreatedBy = currentUserId,
                        CreatedAt = now
                    };
                    newApplications.Add(newApp);

                    // record audit for new pending assignment (assign)
                    auditLogs.Add(new AuditLog
                    {
                        AuditLogId = Guid.NewGuid(),
                        Action = AuditAction.Assign,
                        EntityType = nameof(InternshipApplication),
                        EntityId = newApp.ApplicationId,
                        PerformedById = currentUserId,
                        Metadata = $"{{\"studentId\":\"{sid}\",\"enterpriseId\":\"{request.NewEnterpriseId}\",\"termId\":\"{request.TermId}\",\"note\":\"created pending due to reassignment\"}}"
                    });
                }
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Persist changes
                    if (studentTerm.Any())
                    {
                        foreach (var st in studentTerm)
                        {
                            await _unitOfWork.Repository<StudentTerm>().UpdateAsync(st, cancellationToken);
                        }
                    }

                    if (existingPlacedApps.Any())
                    {
                        foreach (var app in existingPlacedApps)
                        {
                            await _unitOfWork.Repository<InternshipApplication>().UpdateAsync(app, cancellationToken);
                        }
                    }

                    if (newApplications.Any())
                        await _unitOfWork.Repository<InternshipApplication>().AddRangeAsync(newApplications, cancellationToken);

                    // persist audit logs within same transaction
                    if (auditLogs.Any())
                    {
                        await _unitOfWork.Repository<AuditLog>().AddRangeAsync(auditLogs, cancellationToken);
                    }

                    await _unitOfWork.SaveChangeAsync(cancellationToken);

                    // Fetch enterprise name for toast message
                    var enterpriseName = await _unitOfWork.Repository<Enterprise>()
                        .Query()
                        .Where(e => e.EnterpriseId == request.NewEnterpriseId)
                        .Select(e => e.Name)
                        .FirstOrDefaultAsync(cancellationToken) ?? "đơn vị";

                    var assignedCount = remainingStudentIds.Count;
                    var toast = $"Đã gửi chỉ định mới cho {assignedCount} sinh viên sang {enterpriseName}. Đang chờ doanh nghiệp xác nhận.";

                    _logger.LogInformation("Bulk reassigned {Count} students to enterprise {EnterpriseId} by user {UserId}", assignedCount, request.NewEnterpriseId, currentUserId);

                    var responseSuccess = new BulkReassignEnterpriseResponse
                    {
                        Message = toast,
                        AssignedCount = assignedCount,
                        AssignedStudentIds = remainingStudentIds
                    };

                    // AC-07: publish notifications for each student
                    foreach (var newApp in newApplications)
                    {
                        var stu = students.FirstOrDefault(s => s.StudentId == newApp.StudentId);
                        if (stu == null) continue;

                        // If student had a previous placed app, notify ReassignedFromPlaced with old enterprise name
                        var prevPlaced = existingPlacedApps.FirstOrDefault(e => e.StudentId == newApp.StudentId);
                        if (prevPlaced != null)
                        {
                            var oldEntName = await _unitOfWork.Repository<Enterprise>().Query().Where(e => e.EnterpriseId == prevPlaced.EnterpriseId).Select(e => e.Name).FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
                            await _publisher.Publish(new ApplicationReassignedFromPlacedEvent(
                                stu.UserId,
                                newApp.ApplicationId,
                                oldEntName,
                                enterpriseName), cancellationToken);
                        }
                        else
                        {
                            // If there was an existing pending application, this is a reassign from pending
                            var prevPending = existingPendingApps.FirstOrDefault(e => e.StudentId == newApp.StudentId);
                            if (prevPending != null)
                            {
                                await _publisher.Publish(new ApplicationReassignedFromPendingEvent(
                                    stu.UserId,
                                    newApp.ApplicationId,
                                    enterpriseName), cancellationToken);
                            }
                            else
                            {
                                // No previous placed or pending => first-time assignment
                                await _publisher.Publish(new ApplicationAssignedUniAssignEvent(
                                    stu.UserId,
                                    newApp.ApplicationId,
                                    enterpriseName,
                                    term.Name), cancellationToken);
                            }
                        }
                    }

                    return Result<BulkReassignEnterpriseResponse>.Success(responseSuccess, toast);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during bulk reassignment of students to enterprise {EnterpriseId} by user {UserId}", request.NewEnterpriseId, currentUserId);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<BulkReassignEnterpriseResponse>.Failure("An error occurred while reassigning students. Please try again.", ResultErrorType.InternalServerError);
                }
            }
            return Result<BulkReassignEnterpriseResponse>.Failure("You do not have permission to perform this action.", ResultErrorType.Unauthorized);
        }
    }
}
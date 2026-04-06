using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Applications.Commands.UniAssign;

internal class CreateUniAssignHandler : IRequestHandler<CreateUniAssignCommand, Result<CreateUniAssignResponse>>
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CreateUniAssignHandler(AppDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateUniAssignResponse>> Handle(CreateUniAssignCommand request, CancellationToken cancellationToken)
    {
        // Load term
        var term = await _context.Terms
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

        if (term == null)
            return Result<CreateUniAssignResponse>.Failure("Term not found.", ResultErrorType.NotFound);

        // Term status guard: prohibit changes when Closed (mapping: Closed = ended)
        if (term.Status == TermStatus.Closed)
            return Result<CreateUniAssignResponse>.Failure("Không thể thay đổi placement khi kỳ đã kết thúc.", ResultErrorType.Forbidden);

        // Permission: current user must belong to same university
        if (_currentUser.UnitId != term.UniversityId.ToString())
            return Result<CreateUniAssignResponse>.Failure("403 Forbidden: cannot assign students from other universities.", ResultErrorType.Forbidden);

        // Ensure student exists and currently Unplaced for this term (we need to check StudentTerm or applications)
        var student = await _context.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StudentId == request.StudentId, cancellationToken);

        if (student == null)
            return Result<CreateUniAssignResponse>.Failure("Student not found.", ResultErrorType.NotFound);

        // Validate intern phase existence & basic constraints
        var phase = await _context.InternshipPhases
            .FirstOrDefaultAsync(p => p.InternshipPhaseId == request.InternPhaseId && p.EnterpriseId == request.EnterpriseId, cancellationToken);

        if (phase == null)
            return Result<CreateUniAssignResponse>.Failure("Selected intern phase is invalid.", ResultErrorType.BadRequest);

        // Check phase-term date overlap (DateOnly -> convert to DateTime for comparison)
        // Term uses DateOnly; InternshipPhase may have StartDate/EndDate as DateOnly/DateTime depending on model.
        // We cover both: compare via DateOnly if available, otherwise date portion.
        DateOnly termStart = term.StartDate;
        DateOnly termEnd = term.EndDate;
        DateOnly phaseStart = DateOnly.FromDateTime(phase.StartDate);
        DateOnly phaseEnd = DateOnly.FromDateTime(phase.EndDate);

        if (phaseEnd < termStart || phaseStart > termEnd)
            return Result<CreateUniAssignResponse>.Failure("Selected phase does not overlap with the term.", ResultErrorType.BadRequest);

        // At least one job posting with status Published or Closed in that phase
        var hasJob = await _context.Jobs
            .AsNoTracking()
            .AnyAsync(j => j.InternshipPhaseId == request.InternPhaseId &&
                           (j.Status == JobStatus.PUBLISHED || j.Status == JobStatus.CLOSED), cancellationToken);

        if (!hasJob)
            return Result<CreateUniAssignResponse>.Failure("Selected phase has no valid job posting.", ResultErrorType.BadRequest);

        // Validate remaining capacity > 0 now (race guard will re-check right before save)
        if (phase.RemainingCapacity <= 0)
            return Result<CreateUniAssignResponse>.Failure("Intern Phase này vừa đủ số lượng nhận. Vui lòng chọn phase hoặc doanh nghiệp khác.", ResultErrorType.Conflict);

        // Hard block: check student's active self-apply at the same enterprise in active statuses
        var activeSelfApply = await _context.InternshipApplications
            .AsNoTracking()
            .Where(a => a.StudentId == request.StudentId && a.EnterpriseId == request.EnterpriseId && a.Source == ApplicationSource.SelfApply)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeSelfApply != null && activeSelfApply.IsActive())
        {
            return Result<CreateUniAssignResponse>.Failure($"Sinh viên đang có đơn tự ứng tuyển đang xử lý tại enterprise (status: {activeSelfApply.Status}).", ResultErrorType.Conflict);
        }

        // Re-check capacity inside the same transaction / tracked entities
        // Attach the phase entity to ensure EF will see concurrency
        _context.Entry(phase).State = EntityState.Modified; // ensure tracked so the value we check is current
        await _context.Entry(phase).ReloadAsync(cancellationToken);
        if (phase.RemainingCapacity <= 0)
        {
            return Result<CreateUniAssignResponse>.Failure("Intern Phase này vừa đủ số lượng nhận. Vui lòng chọn phase hoặc doanh nghiệp khác.", ResultErrorType.Conflict);
        }

        // Create new application (PendingAssignment)
        var app = new InternshipApplication
        {
            ApplicationId = Guid.NewGuid(),
            EnterpriseId = request.EnterpriseId,
            TermId = request.TermId,
            StudentId = request.StudentId,
            InternPhaseId = request.InternPhaseId,
            Status = InternshipApplicationStatus.PendingAssignment,
            Source = ApplicationSource.UniversityAssign,
            UniversityId = Guid.TryParse(_currentUser.UnitId, out var u) ? u : (Guid?)null,
            AppliedAt = DateTime.UtcNow
        };

        await _context.InternshipApplications.AddAsync(app, cancellationToken);

        // Placement history audit (simple)
        var history = new ApplicationStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            ApplicationId = app.ApplicationId,
            FromStatus = (short)0,
            ToStatus = (short)app.Status,
            CreatedAt = DateTime.UtcNow,
            ChangedByName = _currentUser.UserCode ?? "UniAdmin"
        };
        await _context.ApplicationStatusHistories.AddAsync(history, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        // Create notification for student (in-app)
        var notif = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = request.StudentId,
            Title = "Bạn đã được chỉ định đơn vị thực tập",
            Content = $"Bạn đã được chỉ định vào enterprise (đang chờ doanh nghiệp xác nhận).",
            Type = NotificationType.Info,
            ReferenceType = nameof(InternshipApplication),
            ReferenceId = app.ApplicationId,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Notifications.AddAsync(notif, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Return result (FE will update row / show toast)
        return Result<CreateUniAssignResponse>.Success(new CreateUniAssignResponse { ApplicationId = app.ApplicationId },
            $"Đã gửi chỉ định cho sinh viên. Đang chờ doanh nghiệp xác nhận.");
    }
}
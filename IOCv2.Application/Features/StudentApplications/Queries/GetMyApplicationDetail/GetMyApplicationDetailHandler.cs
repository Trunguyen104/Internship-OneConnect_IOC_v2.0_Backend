using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StudentApplications.Queries.GetMyApplicationDetail;

public class GetMyApplicationDetailHandler : IRequestHandler<GetMyApplicationDetailQuery, Result<GetMyApplicationDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;

    public GetMyApplicationDetailHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
    }

    public async Task<Result<GetMyApplicationDetailResponse>> Handle(GetMyApplicationDetailQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetMyApplicationDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

        var student = await _unitOfWork.Repository<Student>().Query().AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

        if (student == null)
            return Result<GetMyApplicationDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentMessageKey.StudentNotFound), ResultErrorType.NotFound);

        var application = await _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
            .Include(a => a.Enterprise)
            .Include(a => a.Job).ThenInclude(j => j!.InternshipPhase)
            .Include(a => a.StatusHistories.OrderByDescending(h => h.CreatedAt))
            .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId && a.StudentId == student.StudentId, cancellationToken);

        if (application == null || application.IsHiddenByStudent)
            return Result<GetMyApplicationDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentApplications.NotFound), ResultErrorType.NotFound);

        // Map response
        var response = new GetMyApplicationDetailResponse
        {
            ApplicationId = application.ApplicationId,
            Source = application.Source,
            JobTitle = application.Job?.Title,
            JobAudience = application.Job?.Audience.ToString(),
            InternshipPhaseId = application.Job?.InternshipPhaseId,
            InternPhaseName = application.Job?.InternshipPhase?.Name,
            InternPhaseStartDate = application.Job?.InternshipPhase?.StartDate,
            InternPhaseEndDate = application.Job?.InternshipPhase?.EndDate,
            EnterpriseName = application.Enterprise.Name,
            EnterpriseLogoUrl = application.Enterprise.LogoUrl,
            Status = application.Status,
            RejectReason = application.RejectReason,
            AppliedAt = application.AppliedAt,
            CanWithdraw = application.Source == ApplicationSource.SelfApply && application.Status == InternshipApplicationStatus.Applied,
            CanHide = application.Status == InternshipApplicationStatus.Rejected || application.Status == InternshipApplicationStatus.Withdrawn,
            History = application.StatusHistories.Select(h => new ApplicationHistoryDto
            {
                Status = h.ToStatus,
                ChangedAt = h.CreatedAt,
                ChangedByName = h.ChangedByName ?? "System",
                Note = h.TriggerSource == "Student" ? h.Note : null // Do not leak HR notes in MVP, or only leak limited notes.
            }).ToList()
        };

        // Ensure current status is at top if not inside history
        if (!response.History.Any(h => h.Status == response.Status))
        {
             response.History.Insert(0, new ApplicationHistoryDto
             {
                 Status = response.Status,
                 ChangedAt = response.AppliedAt, // Just a fallback for very old records without history
                 ChangedByName = "System",
                 Note = null
             });
        }

        // We only want the history that has happened, filtered down to Student-visible steps:
        // By default, showing the whole history (Applied -> Interviewing -> Offered -> Placed) is good for UX.
        // The notes HR write should probably not be exposed. I have stripped `h.Note` unless it was triggered by Student.

        return Result<GetMyApplicationDetailResponse>.Success(response);
    }
}

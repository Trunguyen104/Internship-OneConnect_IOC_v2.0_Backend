using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.Common.Queries.GetApplicationDetail;

public class GetApplicationDetailHandler : IRequestHandler<GetApplicationDetailQuery, Result<GetApplicationDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetApplicationDetailHandler> _logger;

    public GetApplicationDetailHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetApplicationDetailHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetApplicationDetailResponse>> Handle(
        GetApplicationDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<GetApplicationDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<GetApplicationDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                .Include(a => a.Job).ThenInclude(j => j!.InternPhase)
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.Student).ThenInclude(s => s.StudentTerms).ThenInclude(st => st.Term).ThenInclude(t => t.University)
                .Include(a => a.StatusHistories.OrderBy(h => h.CreatedAt))
                .Include(a => a.University)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == request.ApplicationId &&
                    a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<GetApplicationDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotFound), ResultErrorType.NotFound);

            var latestTerm = app.Student?.StudentTerms?.OrderByDescending(st => st.CreatedAt).FirstOrDefault();

            var response = new GetApplicationDetailResponse
            {
                ApplicationId = app.ApplicationId,
                Source = app.Source,
                SourceLabel = app.Source == ApplicationSource.SelfApply ? "Self-apply" : "Uni Assign",
                StudentId = app.StudentId,
                StudentFullName = app.Student?.User?.FullName ?? string.Empty,
                StudentCode = app.Student?.User?.UserCode ?? string.Empty,
                StudentEmail = app.Student?.User?.Email ?? string.Empty,
                StudentPhone = app.Student?.User?.PhoneNumber ?? string.Empty,
                UniversityName = app.Source == ApplicationSource.UniAssign
                    ? (app.University?.Name ?? string.Empty)
                    : (latestTerm?.Term?.University?.Name ?? string.Empty),
                JobId = app.JobId,
                JobPostingTitle = app.Job?.Title ?? string.Empty,
                IsJobClosed = app.Job?.Status == JobStatus.CLOSED,
                IsJobDeleted = app.JobId.HasValue && app.Job == null,
                CvSnapshotUrl = app.CvSnapshotUrl,
                InternPhaseId = app.Job?.InternPhaseId,
                InternPhaseName = app.Job?.InternPhase?.Name,
                InternPhaseStartDate = app.Job?.InternPhase?.StartDate,
                InternPhaseEndDate = app.Job?.InternPhase?.EndDate,
                Audience = app.Job?.Audience,
                AudienceLabel = app.Job?.Audience?.ToString(),
                Status = app.Status,
                StatusLabel = app.Status.ToString(),
                AppliedAt = app.AppliedAt,
                StatusHistories = app.StatusHistories.Select(h => new StatusHistoryItem
                {
                    FromStatus = h.FromStatus,
                    ToStatus = h.ToStatus,
                    FromStatusLabel = h.FromStatus.ToString(),
                    ToStatusLabel = h.ToStatus.ToString(),
                    Note = h.Note,
                    ChangedByName = h.ChangedByName,
                    TriggerSource = h.TriggerSource,
                    CreatedAt = h.CreatedAt
                }).ToList()
            };

            return Result<GetApplicationDetailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching application detail for {ApplicationId}", request.ApplicationId);
            return Result<GetApplicationDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}

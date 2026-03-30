using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Jobs;
using System.Linq;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobPosting
{
    public class CreateJobPostingHandler : IRequestHandler<CreateJobPostingCommand, Result<CreateJobPostingResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateJobPostingHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public CreateJobPostingHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<CreateJobPostingHandler> logger,
            ICacheService cacheService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
            _messageService = messageService;
        }

        public async Task<Result<CreateJobPostingResponse>> Handle(CreateJobPostingCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("CreateJobPosting started for user unit {UnitId}", _currentUserService.UnitId);

            // Ensure current user is associated with an enterprise (HR)
            if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
            {
                _logger.LogWarning("Current HR user is not associated with an enterprise.");
                return Result<CreateJobPostingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Enterprise.HRNotAssociatedWithEnterprise),
                    ResultErrorType.Forbidden);
            }

            // Create base job via factory (sets Draft status)
            var job = Job.Create(
                enterpriseId: enterpriseId,
                internshipPhase: request.InternshipPhaseId,
                title: request.Title,
                description: request.Description,
                requirements: request.Requirements,
                benefit: request.Benefit,
                location: request.Location,
                quantity: request.Quantity,
                expireDate: request.ExpireDate);

            // Additional properties
            job.Position = request.Position ?? string.Empty;
            job.Audience = request.Audience;

            // Because this command represents publishing, set status to PUBLISHED immediately
            job.Status = JobStatus.PUBLISHED;

            var internshipPhase = await _unitOfWork.Repository<InternshipPhase>().GetByIdAsync(request.InternshipPhaseId, cancellationToken);
            if (internshipPhase == null || internshipPhase.DeletedAt != null)
            {
                _logger.LogWarning("Internship phase {InternshipPhaseId} not found or deleted.", request.InternshipPhaseId);
                return Result<CreateJobPostingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.NotFound),
                    ResultErrorType.NotFound);
            }

            // Only allow selecting phases that are Open (Upcoming) or InProgress (Active)
            if (internshipPhase.Status != InternshipPhaseStatus.Open && internshipPhase.Status != InternshipPhaseStatus.InProgress)
            {
                _logger.LogWarning("Internship phase {InternshipPhaseId} is not available for job postings (status {Status}).", request.InternshipPhaseId, internshipPhase.Status);
                return Result<CreateJobPostingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                    ResultErrorType.BadRequest);
            }

            job.StartDate = internshipPhase.StartDate.ToDateTime(TimeOnly.MinValue);
            job.EndDate = internshipPhase.EndDate.ToDateTime(TimeOnly.MinValue);

            // Validate internship phase date ordering
            if (job.StartDate > job.EndDate)
            {
                _logger.LogWarning("Internship phase {InternshipPhaseId} has StartDate after EndDate ({StartDate} > {EndDate}).", request.InternshipPhaseId, job.StartDate, job.EndDate);
                return Result<CreateJobPostingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                    ResultErrorType.BadRequest);
            }

            // Validate expire date (if provided) using date-only comparison to avoid time-of-day issues
            if (request.ExpireDate.HasValue)
            {
                var expireDate = request.ExpireDate.Value.Date;
                var phaseStart = internshipPhase.StartDate.ToDateTime(TimeOnly.MinValue).Date;
                var phaseEnd = internshipPhase.EndDate.ToDateTime(TimeOnly.MinValue).Date;

                // Expire date must not be after the phase start date (applications must close on/before phase start)
                if (expireDate > phaseStart)
                {
                    _logger.LogWarning("ExpireDate {ExpireDate} cannot be after internship phase start date {StartDate}.", expireDate, phaseStart);
                    return Result<CreateJobPostingResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                        ResultErrorType.BadRequest);
                }

                // Defensive: also ensure expire date is not after phase end (covers unexpected phase ordering)
                if (expireDate > phaseEnd)
                {
                    _logger.LogWarning("ExpireDate {ExpireDate} is after internship phase end date {PhaseEnd}.", expireDate, phaseEnd);
                    return Result<CreateJobPostingResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                        ResultErrorType.BadRequest);
                }
            }

            // If targeted, attach the universities (multi-select)
            if (request.Audience == JobAudience.Targeted)
            {
                if (request.UniversityIds == null || !request.UniversityIds.Any())
                {
                    _logger.LogWarning("Targeted audience but no UniversityIds provided.");
                    return Result<CreateJobPostingResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UniversityRequiredForTargetAudience),
                        ResultErrorType.BadRequest);
                }

                var uniqueIds = request.UniversityIds.Distinct().ToList();
                foreach (var uniId in uniqueIds)
                {
                    var university = await _unitOfWork.Repository<University>().GetByIdAsync(uniId, cancellationToken);
                    if (university == null || university.DeletedAt != null)
                    {
                        _logger.LogWarning("University {UniversityId} not found or deleted.", uniId);
                        return Result<CreateJobPostingResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.University.NotFound),
                            ResultErrorType.NotFound);
                    }

                    job.Universities.Add(university);
                }
            }

            // ----- Duplicate check: prevent creating similar job postings for same enterprise -----
            // Normalize values for comparison
            var normalizedTitle = (request.Title ?? string.Empty).Trim();
            var normalizedPosition = (request.Position ?? string.Empty).Trim();
            var normalizedLocation = (request.Location ?? string.Empty).Trim();

            var duplicateExists = await _unitOfWork.Repository<Job>().ExistsAsync(j =>
                j.EnterpriseId == enterpriseId &&
                (j.Title ?? string.Empty).ToLower() == normalizedTitle.ToLower() &&
                (j.Position ?? string.Empty).ToLower() == normalizedPosition.ToLower() &&
                j.StartDate == job.StartDate &&
                j.EndDate == job.EndDate &&
                j.Audience == job.Audience &&
                (j.Location ?? string.Empty).ToLower() == normalizedLocation.ToLower(),
                cancellationToken);

            if (duplicateExists)
            {
                _logger.LogWarning("Duplicate job posting detected for enterprise {EnterpriseId}, title {Title}", enterpriseId, request.Title);
                return Result<CreateJobPostingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseConflict),
                    ResultErrorType.Conflict);
            }

            // Persist
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            await _unitOfWork.Repository<Job>().AddAsync(job, cancellationToken);
            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error while saving job posting to database.");
                return Result<CreateJobPostingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.InternalServerError);
            }

            var response = _mapper.Map<CreateJobPostingResponse>(job);

            // Return success with draft-saved message (UI toast)
            return Result<CreateJobPostingResponse>.Success(response, _messageService.GetMessage(MessageKeys.JobPostingMessageKey.CreateSuccess));
        }
    }
}

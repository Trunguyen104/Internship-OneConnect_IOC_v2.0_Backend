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
                title: request.Title,
                description: request.Description,
                requirements: request.Requirements,
                benefit: request.Benefit,
                location: request.Location,
                quantity: request.Quantity,
                expireDate: request.ExpireDate);

            // Additional properties
            job.Position = request.Position ?? string.Empty;
            job.StartDate = request.StartDate;
            job.EndDate = request.EndDate;
            job.Audience = request.Audience;

            // Because this command represents publishing, set status to PUBLISHED immediately
            job.Status = JobStatus.PUBLISHED;

            // If targeted, attach the university
            if (request.Audience == JobAudience.Targeted)
            {
                if (request.UniversityId == null)
                {
                    _logger.LogWarning("Targeted audience but no UniversityId provided.");
                    return Result<CreateJobPostingResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                        ResultErrorType.BadRequest);
                }

                var university = await _unitOfWork.Repository<University>().GetByIdAsync(request.UniversityId.Value, cancellationToken);
                if (university == null || university.DeletedAt != null)
                {
                    _logger.LogWarning("University {UniversityId} not found or deleted.", request.UniversityId);
                    return Result<CreateJobPostingResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.University.NotFound),
                        ResultErrorType.NotFound);
                }

                job.Universities.Add(university);
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
                j.StartDate == request.StartDate &&
                j.EndDate == request.EndDate &&
                j.Audience == request.Audience &&
                (j.Location ?? string.Empty).ToLower() == normalizedLocation.ToLower(),
                cancellationToken);

            if (duplicateExists)
            {
                _logger.LogWarning("Duplicate job posting detected for enterprise {EnterpriseId}, title {Title}", enterpriseId, request.Title);
                return Result<CreateJobPostingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseConflict),
                    ResultErrorType.Conflict);
            }
            // -------------------------------------------------------------------------------

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

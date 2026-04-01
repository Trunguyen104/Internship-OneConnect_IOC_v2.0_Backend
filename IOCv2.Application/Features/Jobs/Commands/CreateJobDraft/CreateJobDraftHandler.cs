using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobDraft
{
    public class CreateJobDraftHandler : IRequestHandler<CreateJobDraftCommand, Result<CreateJobDraftResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateJobDraftHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly IMessageService _messageService;

        public CreateJobDraftHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMapper mapper,
            ILogger<CreateJobDraftHandler> logger,
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

        public async Task<Result<CreateJobDraftResponse>> Handle(CreateJobDraftCommand request, CancellationToken cancellationToken)
        {
            _logger.LogDebug("CreateJobDraft started for user unit {UnitId}", _currentUserService.UnitId);

            // Ensure current user is associated with an enterprise (HR)
            if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
            {
                _logger.LogWarning("Current HR user is not associated with an enterprise.");
                return Result<CreateJobDraftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Enterprise.HRNotAssociatedWithEnterprise),
                    ResultErrorType.Forbidden);
            }

            // For auto-save / draft we require at least a Title (AC: auto-save triggers when Title present)
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                _logger.LogWarning("Attempt to save draft without a title.");
                return Result<CreateJobDraftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.JobPostingMessageKey.TitleRequired),
                    ResultErrorType.BadRequest);
            }

            // Create base job via factory (factory sets Draft status)
            var job = Job.Create(
                enterpriseId: enterpriseId,
                internshipPhase: request.InternshipPhaseId,
                title: request.Title,
                description: request.Description,
                requirements: request.Requirements,
                benefit: request.Benefit,
                location: request.Location,
                expireDate: request.ExpireDate);

            if (request.InternshipPhaseId.HasValue && request.InternshipPhaseId != Guid.Empty)
            {
                var internshipPhase = await _unitOfWork.Repository<InternshipPhase>().GetByIdAsync(request.InternshipPhaseId.Value, cancellationToken);
                if (internshipPhase == null || internshipPhase.DeletedAt != null)
                {
                    _logger.LogWarning("Internship phase {InternshipPhaseId} not found or deleted when saving draft.", request.InternshipPhaseId);
                    return Result<CreateJobDraftResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipPhase.NotFound),
                        ResultErrorType.NotFound);
                }

                job.StartDate = DateTime.SpecifyKind(internshipPhase.StartDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
                job.EndDate = DateTime.SpecifyKind(internshipPhase.EndDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            }

            // Additional optional properties for draft
            job.Position = request.Position ?? string.Empty;
            job.Audience = request.Audience;

            // If targeted and university provided, validate and attach; if not provided, still allow draft but log
            if (request.Audience == JobAudience.Targeted)
            {
                if (request.UniversityId.HasValue)
                {
                    var university = await _unitOfWork.Repository<University>().GetByIdAsync(request.UniversityId.Value, cancellationToken);
                    if (university == null || university.DeletedAt != null)
                    {
                        _logger.LogWarning("University {UniversityId} not found or deleted when saving draft.", request.UniversityId);
                        return Result<CreateJobDraftResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.University.NotFound),
                            ResultErrorType.NotFound);
                    }

                    job.Universities.Add(university);
                }
                else
                {
                    // It's acceptable to save a draft without selecting university yet; UI can prompt later.
                    _logger.LogDebug("Targeted audience selected but no UniversityId provided for draft; saving without attached university.");
                }
            }

            // Persist draft
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
                _logger.LogError(ex, "Error while saving job draft to database.");
                return Result<CreateJobDraftResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError),
                    ResultErrorType.InternalServerError);
            }

            var response = _mapper.Map<CreateJobDraftResponse>(job);

            // For auto-save the UI expects a non-intrusive indicator; use a small message
            return Result<CreateJobDraftResponse>.Success(response, _messageService.GetMessage(MessageKeys.JobPostingMessageKey.DraftSavedSuccess));
        }
    }
}

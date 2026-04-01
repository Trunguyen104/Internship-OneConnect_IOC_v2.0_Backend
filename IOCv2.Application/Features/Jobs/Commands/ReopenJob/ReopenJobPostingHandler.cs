using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.ReopenJob
{
    public class ReopenJobPostingHandler : IRequestHandler<ReopenJobPostingCommand, Result<ReopenJobPostingResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ReopenJobPostingHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IPublisher _publisher;

        public ReopenJobPostingHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<ReopenJobPostingHandler> logger,
            IMapper mapper,
            IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
            _mapper = mapper;
            _publisher = publisher;
        }

        public async Task<Result<ReopenJobPostingResponse>> Handle(ReopenJobPostingCommand request, CancellationToken cancellationToken)
        {
            var job = await _unitOfWork.Repository<Job>().Query().FirstOrDefaultAsync(x => x.JobId == request.JobId, cancellationToken);
            if (job == null) return Result<ReopenJobPostingResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            if (job.Status != JobStatus.CLOSED) return Result<ReopenJobPostingResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.InvalidStatusForReopen), ResultErrorType.BadRequest);
            if (request.ExpireDate < DateTime.UtcNow) return Result<ReopenJobPostingResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.ExpireDateMustBeFuture), ResultErrorType.BadRequest);

            var internshipPhase = await _unitOfWork.Repository<InternshipPhase>().Query().FirstOrDefaultAsync(x => x.PhaseId == job.InternshipPhaseId, cancellationToken);
            if (DateOnly.FromDateTime(request.ExpireDate) > internshipPhase!.StartDate) return Result<ReopenJobPostingResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.ExpireDateExceedsPhaseEndDate), ResultErrorType.BadRequest);
            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                job.ExpireDate = request.ExpireDate;
                job.Status = JobStatus.PUBLISHED;
                await _unitOfWork.Repository<Job>().UpdateAsync(job, cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                var response = _mapper.Map<ReopenJobPostingResponse>(job);
                await _publisher.Publish(
                    new JobReopenedEvent(
                        Guid.Parse(_currentUserService.UserId!),
                        job.JobId,
                        job.Title!,
                        job.Enterprise.Name,
                        _messageService.GetMessage(MessageKeys.JobPostingMessageKey.ReopenJobPostingSuccessNotificationMessage)
                    ),
                    cancellationToken
                );
                return Result<ReopenJobPostingResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                _logger.LogError(ex, "An error occurred while reopening job posting with ID {JobId}", request.JobId);
                return Result<ReopenJobPostingResponse>.Failure(ex.Message, ResultErrorType.InternalServerError);
            }
        }
    }
}

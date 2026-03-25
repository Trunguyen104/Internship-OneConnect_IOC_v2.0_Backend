using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJobApplicationStatus
{
    public class UpdateJobApplicationStatusHandler : IRequestHandler<UpdateJobApplicationStatusCommand, Result<UpdateJobApplicationStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundEmailSender _emailSender;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateJobApplicationStatusHandler> _logger;

        public UpdateJobApplicationStatusHandler(
            IUnitOfWork unitOfWork,
            IBackgroundEmailSender emailSender,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<UpdateJobApplicationStatusHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<UpdateJobApplicationStatusResponse>> Handle(UpdateJobApplicationStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentUserService.UnitId) || !Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
                    return Result<UpdateJobApplicationStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

                var repo = _unitOfWork.Repository<JobApplication>();
                var app = await repo.Query()
                    .Include(a => a.Job)
                    .ThenInclude(j => j.Enterprise)
                    .Include(a => a.Student).ThenInclude(s => s.User)
                    .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId, cancellationToken);

                if (app == null)
                    return Result<UpdateJobApplicationStatusResponse>.NotFound("Application not found");

                // Ensure HR belongs to enterprise that owns the job
                if (app.Job.EnterpriseId != enterpriseId)
                    return Result<UpdateJobApplicationStatusResponse>.Failure("You are not allowed to manage this application.", ResultErrorType.Forbidden);

                // Allowed transitions (example; extend to your rules)
                var readOnlyStatuses = new[] { JobApplicationStatus.Accepted /* Accepted maybe Placed */, /* add terminal statuses if needed */ };
                // Terminal statuses (read-only) e.g., Rejected/Withdrawn/Declined/Placed -> disallow changes
                var terminal = new[] { JobApplicationStatus.Accepted, JobApplicationStatus.Rejected /* map other terminal states to enum values if present */ };

                if (terminal.Contains(app.Status))
                {
                    return Result<UpdateJobApplicationStatusResponse>.Failure("Application is in terminal state and cannot be modified.", ResultErrorType.BadRequest);
                }

                // Allow HR actions even if job is Closed or Deleted (per AC-9)
                // Example: perform the status update
                app.Status = request.NewStatus;
                app.UpdatedAt = DateTime.UtcNow;
                // optionally store interview time/note in application entity if fields exist (not shown)
                await _unitOfWork.Repository<JobApplication>().UpdateAsync(app, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Notify student if needed (simple email)
                var studentEmail = app.Student?.User?.Email;
                if (!string.IsNullOrWhiteSpace(studentEmail))
                {
                    var subject = $"Cập nhật trạng thái ứng tuyển: {app.Job?.Title}";
                    var body = $"Trạng thái hồ sơ của bạn đã được cập nhật thành {request.NewStatus}.";
                    _ = _emailSender.EnqueueEmailAsync(studentEmail, subject, body, app.ApplicationId, null, cancellationToken);
                }

                var resp = new UpdateJobApplicationStatusResponse
                {
                    ApplicationId = app.ApplicationId,
                    Status = (short)app.Status,
                    UpdatedAt = app.UpdatedAt
                };

                return Result<UpdateJobApplicationStatusResponse>.Success(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application {ApplicationId}", request.ApplicationId);
                return Result<UpdateJobApplicationStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
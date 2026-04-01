using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.UniAssigns;
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

namespace IOCv2.Application.Features.UniAssign.Commands.QuickEnterpriseAssignment
{
    internal class QuickEnterpriseAssignmentHandler : IRequestHandler<QuickEnterpriseAssignmentCommand, Result<QuickEnterpriseAssignmentResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<QuickEnterpriseAssignmentHandler> _logger;
        private readonly IMessageService _messageService;

        public QuickEnterpriseAssignmentHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<QuickEnterpriseAssignmentHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<QuickEnterpriseAssignmentResponse>> Handle(QuickEnterpriseAssignmentCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = Guid.Parse(_currentUser.UserId!);
            if (UniAssignParam.CreateUniAssignParam.UniAllowedRole.Contains(_currentUser.Role)) {
                var currentUserUniversityId = await _unitOfWork.Repository<UniversityUser>().Query().Where(x => x.UserId == currentUserId).Select(x => x.UniversityId).FirstOrDefaultAsync(cancellationToken);
                var studentUserId = await _unitOfWork.Repository<Student>().Query().Where(x => x.StudentId == request.StudentId).Select(x => x.UserId).FirstOrDefaultAsync(cancellationToken);
                var studentUniversityId = await _unitOfWork.Repository<UniversityUser>().Query().Where(x => x.UserId == studentUserId).Select(x => x.UniversityId).FirstOrDefaultAsync(cancellationToken);
                if (currentUserUniversityId != studentUniversityId) {
                    return Result<QuickEnterpriseAssignmentResponse>.Failure("You are not allowed to assign this student.", ResultErrorType.Unauthorized);
                }
            }

            var term = await _unitOfWork.Repository<Term>().GetByIdAsync(request.TermId, cancellationToken);
            if (term == null) {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Term not found.", ResultErrorType.NotFound);
            }
            if (term.Status != TermStatus.Open) {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Term is not open for assignment.", ResultErrorType.BadRequest);
            }

            var internshipPhase = await _unitOfWork.Repository<InternshipPhase>().Query().Where(x => x.PhaseId == request.InternPhaseId).FirstOrDefaultAsync(cancellationToken);
            if (internshipPhase == null)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Internship phase not found.", ResultErrorType.NotFound);
            }
            // Validate phase date ordering
            if (internshipPhase.StartDate > internshipPhase.EndDate)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Internship phase start date is after its end date.", ResultErrorType.BadRequest);
            }
            // Ensure the internship phase is within the term date range
            if (internshipPhase.StartDate < term.StartDate || internshipPhase.EndDate > term.EndDate)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Internship phase dates must be within the term start and end dates.", ResultErrorType.BadRequest);
            }
            // Ensure the internship phase has at least one job posting with status Published or Closed.
            var hasJobPosting = await _unitOfWork.Repository<Job>().Query()
                .Where(j => j.InternshipPhaseId == internshipPhase.PhaseId
                            && (j.Status == JobStatus.PUBLISHED || j.Status == JobStatus.CLOSED))
                .AnyAsync(cancellationToken);
            if (!hasJobPosting)
            {
                return Result<QuickEnterpriseAssignmentResponse>.Failure("Internship phase must have at least one published or closed job posting.", ResultErrorType.BadRequest);
            }

            // TODO: Continue handler implementation (create uni-assign, persist, send messages, return success).
            return Result<QuickEnterpriseAssignmentResponse>.Failure("Handler not fully implemented after validations.", ResultErrorType.BadRequest);
        }
    }
}
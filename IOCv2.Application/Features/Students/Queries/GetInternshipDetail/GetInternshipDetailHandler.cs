using MediatR;
using Microsoft.EntityFrameworkCore;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Students.Queries.GetInternshipDetail
{
    public class GetInternshipDetailHandler : IRequestHandler<GetInternshipDetailQuery, Result<GetInternshipDetailResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetInternshipDetailHandler> _logger;

        public GetInternshipDetailHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, ILogger<GetInternshipDetailHandler> logger)
            => (_unitOfWork, _currentUserService, _messageService, _logger) = (unitOfWork, currentUserService, messageService, logger);

        public async Task<Result<GetInternshipDetailResponse>> Handle(GetInternshipDetailQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Students.LogUnauthorizedAccess, _currentUserService.UserId));
                return Result<GetInternshipDetailResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized));
            }

            var studentId = await _unitOfWork.Repository<Student>().Query()
                .Where(s => s.UserId == userId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentId == Guid.Empty)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Students.LogProfileNotFound, userId));
                return Result<GetInternshipDetailResponse>.Failure(_messageService.GetMessage(MessageKeys.Students.ProfileNotFound));
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = from st in _unitOfWork.Repository<StudentTerm>().Query().AsNoTracking()
                        where st.StudentId == studentId && st.TermId == request.TermId
                        join t in _unitOfWork.Repository<Term>().Query().AsNoTracking() on st.TermId equals t.TermId

                        let placement = (
                            from is_member in _unitOfWork.Repository<InternshipStudent>().Query().AsNoTracking()
                            where is_member.StudentId == st.StudentId
                            join ig in _unitOfWork.Repository<InternshipGroup>().Query().AsNoTracking()
                                on is_member.InternshipId equals ig.InternshipId
                            where ig.TermId == st.TermId

                            join e in _unitOfWork.Repository<Enterprise>().Query().AsNoTracking()
                                on ig.EnterpriseId equals e.EnterpriseId into eGroup
                            from e in eGroup.DefaultIfEmpty()

                            select new { ig, e }
                        ).FirstOrDefault()

                        select new GetInternshipDetailResponse
                        {
                            TermId = t.TermId,
                            TermName = t.Name,
                            TermStatus = t.Status.ToString(),
                            StartDate = t.StartDate,
                            EndDate = t.EndDate,
                            DaysUntilStart = t.StartDate > today ? t.StartDate.DayNumber - today.DayNumber : null,
                            DaysUntilEnd = t.EndDate > today ? t.EndDate.DayNumber - today.DayNumber : null,
                            IsPlaced = placement != null && placement.ig != null,
                            EnterpriseName = placement != null && placement.e != null ? placement.e.Name : _messageService.GetMessage(MessageKeys.Students.EnterpriseNotAssigned),
                            EnrollmentStatus = st.Status.HasValue ? st.Status.ToString() : "Unknown"
                        };

            var result = await query.FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                // Anti-IDOR: either Term doesn't exist, or student is not enrolled in this term
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Students.LogTermNotFoundOrNoAccess, request.TermId, studentId));
                return Result<GetInternshipDetailResponse>.Failure(_messageService.GetMessage(MessageKeys.Students.TermNotFoundOrNoAccess));
            }

            // Post-query mapping for Placement Badge and Messages
            if (result.IsPlaced)
            {
                result.PlacementBadge = _messageService.GetMessage(MessageKeys.Students.BadgePlaced);
            }
            else
            {
                result.PlacementBadge = _messageService.GetMessage(MessageKeys.Students.BadgeUnplaced);
                result.EnterpriseName = _messageService.GetMessage(MessageKeys.Students.EnterpriseNotAssigned);

                if (result.TermStatus == TermStatus.Upcoming.ToString())
                {
                    result.PlacementMessage = _messageService.GetMessage(MessageKeys.Students.MessageApplyToEnterprise);
                }
                else if (result.TermStatus == TermStatus.Active.ToString())
                {
                    result.PlacementMessage = _messageService.GetMessage(MessageKeys.Students.MessageContactAdmin);
                }
            }

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Students.LogGetInternshipDetailSuccess, request.TermId, studentId));

            return Result<GetInternshipDetailResponse>.Success(result);
        }
    }
}

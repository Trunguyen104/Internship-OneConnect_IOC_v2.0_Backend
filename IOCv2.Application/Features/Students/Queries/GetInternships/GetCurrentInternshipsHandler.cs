using MediatR;
using Microsoft.EntityFrameworkCore;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Students.Queries.GetInternships
{
    public class GetCurrentInternshipsHandler : IRequestHandler<GetCurrentInternshipsQuery, Result<List<GetCurrentInternshipsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetCurrentInternshipsHandler> _logger;

        public GetCurrentInternshipsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMessageService messageService, ILogger<GetCurrentInternshipsHandler> logger)
            => (_unitOfWork, _currentUserService, _messageService, _logger) = (unitOfWork, currentUserService, messageService, logger);

        public async Task<Result<List<GetCurrentInternshipsResponse>>> Handle(GetCurrentInternshipsQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Students.LogUnauthorizedAccess, _currentUserService.UserId));
                return Result<List<GetCurrentInternshipsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized));
            }

            var studentId = await _unitOfWork.Repository<Student>().Query()
                .Where(s => s.UserId == userId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentId == Guid.Empty)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Students.LogProfileNotFound, userId));
                return Result<List<GetCurrentInternshipsResponse>>.Failure(_messageService.GetMessage(MessageKeys.Students.ProfileNotFound));
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = from st in _unitOfWork.Repository<StudentTerm>().Query().AsNoTracking()
                        where st.StudentId == studentId
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

                            join mentor in _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                                on ig.MentorId equals mentor.EnterpriseUserId into mGroup
                            from ment in mGroup.DefaultIfEmpty()

                            join mentorUser in _unitOfWork.Repository<User>().Query().AsNoTracking()
                                on ment.UserId equals mentorUser.UserId into muGroup
                            from mu in muGroup.DefaultIfEmpty()

                            join p in _unitOfWork.Repository<Project>().Query().AsNoTracking()
                                on ig.InternshipId equals p.InternshipId into pGroup
                            from proj in pGroup.DefaultIfEmpty()

                            select new { ig, e, mu, proj }
                        ).FirstOrDefault()

                        orderby st.CreatedAt descending
                        select new GetCurrentInternshipsResponse
                        {
                            TermId = t.TermId,
                            TermName = t.Name,
                            TermStatus = t.Status.ToString(),
                            StartDate = t.StartDate,
                            EndDate = t.EndDate,
                            DaysUntilStart = t.StartDate > today ? t.StartDate.DayNumber - today.DayNumber : null,
                            DaysUntilEnd = t.EndDate > today ? t.EndDate.DayNumber - today.DayNumber : null,
                            IsPlaced = placement != null,
                            EnterpriseName = placement != null && placement.e != null ? placement.e.Name : _messageService.GetMessage(MessageKeys.Students.EnterpriseNotAssigned),
                            MentorName = (t.Status == TermStatus.Active && placement != null && placement.mu != null) ? placement.mu.FullName : null,
                            ProjectName = (t.Status == TermStatus.Active && placement != null && placement.proj != null) ? placement.proj.ProjectName : null,
                            EnrollmentStatus = st.Status.HasValue ? st.Status.ToString() : "Unknown"
                        };

            var list = await query.ToListAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Students.LogGetInternshipsSuccess, list.Count, studentId));

            return Result<List<GetCurrentInternshipsResponse>>.Success(list);
        }
    }
}

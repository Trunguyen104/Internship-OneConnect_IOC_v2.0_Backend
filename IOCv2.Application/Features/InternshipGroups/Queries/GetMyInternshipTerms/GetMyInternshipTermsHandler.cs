using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipTerms;

public class GetMyInternshipTermsHandler : IRequestHandler<GetMyInternshipTermsQuery, Result<List<GetMyInternshipTermsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetMyInternshipTermsHandler> _logger;

    public GetMyInternshipTermsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetMyInternshipTermsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<List<GetMyInternshipTermsResponse>>> Handle(GetMyInternshipTermsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting get my internship terms query.");

        if (string.IsNullOrWhiteSpace(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            throw new UnauthorizedAccessException(_messageService.GetMessage(MessageKeys.Common.Unauthorized));
        }

        var student = await _unitOfWork.Repository<Student>()
            .Query()
            .Where(s => s.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        if (student == null)
        {
            throw new NotFoundException(_messageService.GetMessage(MessageKeys.Users.NotFound));
        }

        var now = DateTime.UtcNow;
        var nowDateOnly = DateOnly.FromDateTime(now);

        var terms = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Term)
            .Where(st => st.StudentId == student.StudentId)
            .OrderByDescending(st => st.Term.StartDate)
            .Select(st => new {
                StudentTerm = st,
                Term = st.Term
            })
            .ToListAsync(cancellationToken);

        // Get group info for these terms
        var termIds = terms.Select(t => t.Term.TermId).ToList();
        var groups = await _unitOfWork.Repository<InternshipGroup>()
            .Query()
            .Include(g => g.Enterprise)
            .Include(g => g.Mentor)
                .ThenInclude(m => m!.User)
            .Include(g => g.Members)
            .Where(g => termIds.Contains(g.TermId) && g.Members.Any(m => m.StudentId == student.StudentId))
            .ToListAsync(cancellationToken);

        var groupLookup = groups.ToDictionary(g => g.TermId);

        var response = terms.Select(t => {
            var term = t.Term;
            var st = t.StudentTerm;
            var group = groupLookup.GetValueOrDefault(term.TermId);

            var displayStatus = CalculateDisplayStatus(term, nowDateOnly);
            
            // EnrollmentStatus mapping (Default to Active if null)
            var enrollmentStatus = (EnrollmentStatus)(st.Status ?? (short)EnrollmentStatus.Active);

            int journeyStep = CalculateJourneyStep(displayStatus, group != null);

            return new GetMyInternshipTermsResponse
            {
                TermId = term.TermId,
                TermName = term.Name,
                Status = displayStatus,
                EnrollmentStatus = enrollmentStatus,
                IsPlaced = group != null,
                InternshipGroupId = group?.InternshipId,
                EnterpriseName = group?.Enterprise?.Name,
                MentorName = group?.Mentor?.User?.FullName,
                ProjectName = group?.GroupName, // Fallback to GroupName if project not specifically joined
                JourneyStep = journeyStep,
                StartDate = term.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = term.EndDate.ToDateTime(TimeOnly.MinValue),
                EnrolledAt = st.CreatedAt
            };
        }).ToList();

        return Result<List<GetMyInternshipTermsResponse>>.Success(response);
    }

    private static TermDisplayStatus CalculateDisplayStatus(Term term, DateOnly now)
    {
        if (term.Status == TermStatus.Closed) return TermDisplayStatus.Closed;
        if (now < term.StartDate) return TermDisplayStatus.Upcoming;
        if (now > term.EndDate) return TermDisplayStatus.Ended;
        return TermDisplayStatus.Active;
    }

    private static int CalculateJourneyStep(TermDisplayStatus status, bool isPlaced)
    {
        if (status == TermDisplayStatus.Closed || status == TermDisplayStatus.Ended) return 5;
        if (isPlaced) return 4;
        // Simplified: if active/upcoming but not placed, assume step 1 or 2
        return status == TermDisplayStatus.Active ? 2 : 1;
    }
}

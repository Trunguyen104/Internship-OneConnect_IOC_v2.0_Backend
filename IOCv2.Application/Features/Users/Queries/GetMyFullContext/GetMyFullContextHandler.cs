using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Users.Queries.GetMyFullContext;

public class GetMyFullContextHandler : IRequestHandler<GetMyFullContextQuery, Result<GetMyFullContextResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetMyFullContextHandler> _logger;

    public GetMyFullContextHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetMyFullContextHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetMyFullContextResponse>> Handle(GetMyFullContextQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Retrieving full context for current student");

        if (string.IsNullOrWhiteSpace(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            throw new UnauthorizedAccessException(_messageService.GetMessage(MessageKeys.Common.Unauthorized));
        }

        var student = await _unitOfWork.Repository<Student>()
            .Query()
            .Where(s => s.UserId == userId)
            .Include(s => s.StudentTerms)
                .ThenInclude(st => st.Term)
                    .ThenInclude(t => t.University)
            .Include(s => s.InternshipStudents)
                .ThenInclude(ist => ist.InternshipGroup)
                    .ThenInclude(g => g.Enterprise)
            .Include(s => s.InternshipStudents)
                .ThenInclude(ist => ist.InternshipGroup)
                    .ThenInclude(g => g.Mentor)
                        .ThenInclude(m => m!.User)
            .Include(s => s.InternshipStudents)
                .ThenInclude(ist => ist.InternshipGroup)
                    .ThenInclude(g => g.InternshipPhase)
            .Include(s => s.InternshipStudents)
                .ThenInclude(ist => ist.InternshipGroup)
                    .ThenInclude(g => g.Projects)
            .FirstOrDefaultAsync(cancellationToken);

        if (student == null)
        {
            throw new NotFoundException(_messageService.GetMessage(MessageKeys.Users.NotFound));
        }

        var response = new GetMyFullContextResponse
        {
            StudentInfo = new StudentContextInfo
            {
                StudentId = student.StudentId,
                ClassName = student.ClassName,
                Major = student.Major,
                Gpa = student.Gpa
            }
        };

        var now = DateTime.UtcNow;
        var nowDateOnly = DateOnly.FromDateTime(now);

        // Find the active or most recent term
        var activeOrMostRecentTerm = student.StudentTerms
            .OrderByDescending(st => st.Term.StartDate)
            .FirstOrDefault();

        if (activeOrMostRecentTerm != null)
        {
            var term = activeOrMostRecentTerm.Term;
            response.University = new UniversityContextInfo
            {
                UniversityId = term.UniversityId,
                Name = term.University?.Name ?? "Unknown University"
            };

            response.CurrentTerm = new TermContextInfo
            {
                TermId = term.TermId,
                TermName = term.Name,
                StartDate = term.StartDate.ToDateTime(TimeOnly.MinValue),
                EndDate = term.EndDate.ToDateTime(TimeOnly.MinValue),
                Status = CalculateDisplayStatus(term, nowDateOnly),
                EnrollmentStatus = activeOrMostRecentTerm.EnrollmentStatus
            };
        }

        // Find the most relevant internship group
        // If they have a term, prefer a group from a phase that belongs to that term.
        // Wait, phase links to term via... oh, InternshipPhase has TermId. Let's see if we can just pick the first group or a group related to the active term.
        var firstInternshipStudent = student.InternshipStudents
            .OrderByDescending(ist => ist.JoinedAt)
            .FirstOrDefault();

        if (firstInternshipStudent?.InternshipGroup != null)
        {
            var group = firstInternshipStudent.InternshipGroup;
            response.Internship = new InternshipContextInfo
            {
                Group = new GroupContextInfo
                {
                    GroupId = group.InternshipId,
                    GroupName = group.GroupName
                }
            };

            if (group.InternshipPhase != null)
            {
                response.Internship.Phase = new InternshipPhaseContextInfo
                {
                    PhaseId = group.PhaseId,
                    Name = group.InternshipPhase.Name,
                    Status = group.InternshipPhase.Status
                };
            }

            if (group.Enterprise != null)
            {
                response.Internship.Enterprise = new EnterpriseContextInfo
                {
                    EnterpriseId = group.EnterpriseId!.Value,
                    Name = group.Enterprise.Name
                };
            }

            if (group.Mentor?.User != null)
            {
                response.Internship.Mentor = new MentorContextInfo
                {
                    MentorId = group.MentorId!.Value,
                    Name = group.Mentor.User.FullName,
                    Email = group.Mentor.User.Email
                };
            }

            var project = group.Projects.FirstOrDefault(); // Just take the first active project if any
            if (project != null)
            {
                response.Internship.Project = new ProjectContextInfo
                {
                    ProjectId = project.ProjectId,
                    Name = project.ProjectName,
                    Status = project.OperationalStatus
                };
            }
        }

        return Result<GetMyFullContextResponse>.Success(response);
    }

    private static TermDisplayStatus CalculateDisplayStatus(Term term, DateOnly now)
    {
        if (term.Status == TermStatus.Closed) return TermDisplayStatus.Closed;
        if (now < term.StartDate) return TermDisplayStatus.Upcoming;
        if (now > term.EndDate) return TermDisplayStatus.Ended;
        return TermDisplayStatus.Active;
    }
}

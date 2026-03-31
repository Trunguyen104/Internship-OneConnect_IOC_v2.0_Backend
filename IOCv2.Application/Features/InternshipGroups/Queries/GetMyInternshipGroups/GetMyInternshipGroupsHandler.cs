using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipGroups;

public class GetMyInternshipGroupsHandler : IRequestHandler<GetMyInternshipGroupsQuery, Result<List<GetMyInternshipGroupsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetMyInternshipGroupsHandler> _logger;

    public GetMyInternshipGroupsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetMyInternshipGroupsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<List<GetMyInternshipGroupsResponse>>> Handle(GetMyInternshipGroupsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogStartQueryMine));

        if (string.IsNullOrWhiteSpace(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogQueryMineDenied));
            throw new UnauthorizedAccessException(_messageService.GetMessage(MessageKeys.Common.Unauthorized));
        }

        var student = await _unitOfWork.Repository<Student>()
            .Query()
            .Include(s => s.User!).ThenInclude(u => u.UniversityUser!).ThenInclude(uu => uu.University)
            .Where(s => s.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (student == null)
        {
            _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogQueryMineStudentNotFound));
            throw new NotFoundException(_messageService.GetMessage(MessageKeys.Users.NotFound));
        }

        var studentId = student.StudentId;
        var university = student.User?.UniversityUser?.University;

        var groups = await _unitOfWork.Repository<InternshipGroup>()
            .Query()
            .Include(group => group.Enterprise)
            .Include(group => group.Mentor)
                .ThenInclude(mentor => mentor!.User)
            .Include(group => group.InternshipPhase)
            .Include(group => group.Members)
            .Where(group => group.DeletedAt == null && group.Members.Any(member => member.StudentId == studentId))
            .OrderByDescending(group => group.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var internshipIds = groups.Select(group => group.InternshipId).ToList();

        var projectLookup = internshipIds.Count == 0
            ? new Dictionary<Guid, Project>()
            : (await _unitOfWork.Repository<Project>()
                .Query()
                .Where(project => project.DeletedAt == null && project.InternshipId.HasValue && internshipIds.Contains(project.InternshipId.Value))
                .OrderByDescending(project => project.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken))
                .GroupBy(project => project.InternshipId!.Value)
                .ToDictionary(group => group.Key, group => group.First());


        var phaseIds = groups.Where(group => group.PhaseId.HasValue).Select(group => group.PhaseId!.Value).Distinct().ToList();
        var evaluationCycleLookup = phaseIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _unitOfWork.Repository<EvaluationCycle>()
                .Query()
                .Where(cycle => phaseIds.Contains(cycle.PhaseId))
                .GroupBy(cycle => cycle.PhaseId)
                .Select(group => new { PhaseId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.PhaseId, x => x.Count, cancellationToken);

        var response = groups
            .Select(group => {
                var res = GetMyInternshipGroupsResponse.FromEntity(
                    group,
                    projectLookup.GetValueOrDefault(group.InternshipId),
                    university);
                res.EvaluationCount = group.PhaseId.HasValue
                    ? evaluationCycleLookup.GetValueOrDefault(group.PhaseId.Value)
                    : 0;
                return res;
            })
            .ToList();

        _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogQueryMineCompleted), response.Count);

        return Result<List<GetMyInternshipGroupsResponse>>.Success(response);
    }
}

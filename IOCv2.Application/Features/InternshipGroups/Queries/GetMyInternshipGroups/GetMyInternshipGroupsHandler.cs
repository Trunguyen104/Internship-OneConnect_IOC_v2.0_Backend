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
        _logger.LogInformation("Starting mine internship groups query.");

        if (string.IsNullOrWhiteSpace(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            _logger.LogWarning("Mine internship groups query denied because current user is unavailable.");
            throw new UnauthorizedAccessException(_messageService.GetMessage(MessageKeys.Common.Unauthorized));
        }

        var studentId = await _unitOfWork.Repository<Student>()
            .Query()
            .Where(student => student.UserId == userId)
            .Select(student => (Guid?)student.StudentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!studentId.HasValue)
        {
            _logger.LogWarning("Mine internship groups query failed because student profile was not found for current user.");
            throw new NotFoundException(_messageService.GetMessage(MessageKeys.Users.NotFound));
        }

        var groups = await _unitOfWork.Repository<InternshipGroup>()
            .Query()
            .Include(group => group.Enterprise)
            .Include(group => group.Mentor)
                .ThenInclude(mentor => mentor!.User)
            .Include(group => group.Term)
                .ThenInclude(term => term.University)
            .Include(group => group.Members)
            .Where(group => group.DeletedAt == null && group.Members.Any(member => member.StudentId == studentId.Value))
            .OrderByDescending(group => group.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var internshipIds = groups.Select(group => group.InternshipId).ToList();

        var projectLookup = internshipIds.Count == 0
            ? new Dictionary<Guid, Project>()
            : (await _unitOfWork.Repository<Project>()
                .Query()
                .Where(project => project.DeletedAt == null && internshipIds.Contains(project.InternshipId))
                .OrderByDescending(project => project.CreatedAt)
                .AsNoTracking()
                .ToListAsync(cancellationToken))
                .GroupBy(project => project.InternshipId)
                .ToDictionary(group => group.Key, group => group.First());


        var termIds = groups.Select(group => group.TermId).Distinct().ToList();
        var evaluationCycleLookup = termIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _unitOfWork.Repository<EvaluationCycle>()
                .Query()
                .Where(cycle => termIds.Contains(cycle.TermId))
                .GroupBy(cycle => cycle.TermId)
                .Select(group => new { TermId = group.Key, Count = group.Count() })
                .ToDictionaryAsync(x => x.TermId, x => x.Count, cancellationToken);

        var response = groups
            .Select(group => {
                var res = GetMyInternshipGroupsResponse.FromEntity(
                    group,
                    projectLookup.GetValueOrDefault(group.InternshipId));
                res.EvaluationCount = evaluationCycleLookup.GetValueOrDefault(group.TermId);
                return res;
            })
            .ToList();

        _logger.LogInformation("Completed mine internship groups query with {Count} groups.", response.Count);

        return Result<List<GetMyInternshipGroupsResponse>>.Success(response);
    }
}

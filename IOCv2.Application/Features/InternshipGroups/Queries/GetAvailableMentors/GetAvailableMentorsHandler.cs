using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetAvailableMentors;

public class GetAvailableMentorsHandler
    : IRequestHandler<GetAvailableMentorsQuery, Result<List<AvailableMentorDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetAvailableMentorsHandler> _logger;

    public GetAvailableMentorsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetAvailableMentorsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<List<AvailableMentorDto>>> Handle(
        GetAvailableMentorsQuery request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<List<AvailableMentorDto>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                ResultErrorType.Unauthorized);

        if (!Enum.TryParse<UserRole>(_currentUserService.Role, true, out var currentRole) || currentRole != UserRole.HR)
            return Result<List<AvailableMentorDto>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Forbidden),
                ResultErrorType.Forbidden);

        var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

        if (enterpriseUser == null)
            return Result<List<AvailableMentorDto>>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                ResultErrorType.Forbidden);

        var group = await _unitOfWork.Repository<InternshipGroup>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.InternshipId == request.InternshipGroupId && g.DeletedAt == null, cancellationToken);

        if (group == null)
        {
            _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipGroupId);
            return Result<List<AvailableMentorDto>>.NotFound(
                _messageService.GetMessage(MessageKeys.InternshipGroups.AssignMentorGroupNotFound));
        }

        if (group.EnterpriseId != enterpriseUser.EnterpriseId)
        {
            _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogGetAvailableMentorsAccessDenied),
                currentUserId, request.InternshipGroupId);
            return Result<List<AvailableMentorDto>>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                ResultErrorType.Forbidden);
        }

        if (group.Status == GroupStatus.Archived || (group.EndDate.HasValue && group.EndDate.Value < DateTime.UtcNow))
            return Result<List<AvailableMentorDto>>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.AssignMentorGroupNotActive),
                ResultErrorType.BadRequest);

        // Load tất cả mentors thuộc enterprise này
        var mentors = await _unitOfWork.Repository<EnterpriseUser>().Query()
            .Include(eu => eu.User)
            .AsNoTracking()
            .Where(eu => eu.EnterpriseId == enterpriseUser.EnterpriseId
                      && eu.User.Role == UserRole.Mentor
                      && eu.User.Status == UserStatus.Active
                      && eu.User.DeletedAt == null)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.Trim().ToLower();
            mentors = mentors
                .Where(m => m.User.FullName.ToLower().Contains(searchTerm)
                         || m.User.Email.ToLower().Contains(searchTerm))
                .ToList();
        }

        // Đếm số nhóm Active mỗi mentor đang phụ trách — 1 query GroupBy
        var mentorEnterpriseUserIds = mentors.Select(m => m.EnterpriseUserId).ToList();
        var groupCountByMentor = await _unitOfWork.Repository<InternshipGroup>().Query()
            .AsNoTracking()
            .Where(g => g.MentorId.HasValue
                     && mentorEnterpriseUserIds.Contains(g.MentorId.Value)
                     && g.Status == GroupStatus.Active
                     && g.DeletedAt == null)
            .GroupBy(g => g.MentorId!.Value)
            .Select(g => new { MentorId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.MentorId, x => x.Count, cancellationToken);

        var result = mentors
            .Select(m => new AvailableMentorDto
            {
                UserId = m.UserId,
                EnterpriseUserId = m.EnterpriseUserId,
                FullName = m.User.FullName,
                Email = m.User.Email,
                Position = m.Position,
                CurrentGroupCount = groupCountByMentor.TryGetValue(m.EnterpriseUserId, out var cnt) ? cnt : 0,
                IsCurrentMentor = m.EnterpriseUserId == group.MentorId
            })
            .OrderByDescending(m => m.IsCurrentMentor)   // mentor hiện tại lên đầu (để FE disabled)
            .ThenBy(m => m.CurrentGroupCount)
            .ThenBy(m => m.FullName)
            .ToList();

        _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogGetAvailableMentors),
            result.Count, request.InternshipGroupId);

        return Result<List<AvailableMentorDto>>.Success(
            result,
            _messageService.GetMessage(MessageKeys.InternshipGroups.AvailableMentorsRetrieved));
    }
}

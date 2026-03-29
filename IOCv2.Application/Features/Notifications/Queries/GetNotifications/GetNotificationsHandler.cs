using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsHandler : IRequestHandler<GetNotificationsQuery, Result<PaginatedResult<GetNotificationsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetNotificationsHandler> _logger;

    public GetNotificationsHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICurrentUserService currentUserService,
        ILogger<GetNotificationsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<GetNotificationsResponse>>> Handle(
        GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<PaginatedResult<GetNotificationsResponse>>.Failure(
                "Unauthorized", ResultErrorType.Unauthorized);

        _logger.LogInformation("Getting notifications for user {UserId}, Page {Page}", currentUserId, request.PageNumber);

        var query = _unitOfWork.Repository<Notification>().Query()
            .AsNoTracking()
            .Where(n => n.UserId == currentUserId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<GetNotificationsResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        var result = PaginatedResult<GetNotificationsResponse>.Create(
            items, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedResult<GetNotificationsResponse>>.Success(result);
    }
}

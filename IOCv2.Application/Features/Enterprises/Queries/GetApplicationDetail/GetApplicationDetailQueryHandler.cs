using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Enterprises.Queries.GetApplicationDetail;

public class GetApplicationDetailQueryHandler : IRequestHandler<GetApplicationDetailQuery, Result<GetApplicationDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetApplicationDetailQueryHandler> _logger;

    public GetApplicationDetailQueryHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetApplicationDetailQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<GetApplicationDetailResponse>> Handle(GetApplicationDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserIdStr = _currentUserService.UserId;
            if (!Guid.TryParse(currentUserIdStr, out var currentUserId))
                return Result<GetApplicationDetailResponse>.Failure("User ID không hợp lệ.", ResultErrorType.Unauthorized);

            var currentUserRole = _currentUserService.Role;

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<GetApplicationDetailResponse>.Failure("Không tìm thấy thông tin nhân viên doanh nghiệp.", ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                .Include(a => a.Student).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId &&
                                          a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<GetApplicationDetailResponse>.Failure("Không tìm thấy đơn ứng tuyển.", ResultErrorType.NotFound);

            // Mentor: only see students in their groups
            if (currentUserRole == UserRole.Mentor.ToString())
            {
                var mentorStudentIds = await _unitOfWork.Repository<InternshipGroup>().Query().AsNoTracking()
                    .Where(ig => ig.EnterpriseId == enterpriseUser.EnterpriseId &&
                                 ig.TermId == app.TermId &&
                                 ig.MentorId == enterpriseUser.EnterpriseUserId)
                    .SelectMany(ig => ig.Members)
                    .Select(m => m.StudentId)
                    .ToListAsync(cancellationToken);

                if (!mentorStudentIds.Contains(app.StudentId))
                    return Result<GetApplicationDetailResponse>.Failure("Bạn không có quyền xem đơn này.", ResultErrorType.Forbidden);
            }

            var membership = await _unitOfWork.Repository<InternshipStudent>().Query().AsNoTracking()
                .Include(ms => ms.InternshipGroup).ThenInclude(ig => ig.Mentor).ThenInclude(m => m.User)
                .Include(ms => ms.InternshipGroup).ThenInclude(ig => ig.Projects)
                .FirstOrDefaultAsync(ms => ms.StudentId == app.StudentId &&
                                            ms.InternshipGroup.EnterpriseId == enterpriseUser.EnterpriseId &&
                                            ms.InternshipGroup.TermId == app.TermId, cancellationToken);

            var dto = new GetApplicationDetailResponse
            {
                ApplicationId = app.ApplicationId,
                EnterpriseId = app.EnterpriseId,
                TermId = app.TermId,
                StudentId = app.StudentId,
                StudentFullName = app.Student?.User?.FullName ?? "N/A",
                StudentCode = app.Student?.User?.UserCode ?? "N/A",
                Email = app.Student?.User?.Email ?? "N/A",
                Phone = app.Student?.User?.PhoneNumber ?? string.Empty,
                UniversityName = "Unknown",
                Major = app.Student?.Major ?? string.Empty,
                Status = app.Status,
                RejectReason = app.RejectReason,
                MentorName = membership?.InternshipGroup?.Mentor?.User?.FullName,
                ProjectName = membership?.InternshipGroup?.Projects?.FirstOrDefault()?.ProjectName,
                AppliedAt = app.AppliedAt
            };

            return Result<GetApplicationDetailResponse>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching application detail");
            return Result<GetApplicationDetailResponse>.Failure("Đã xảy ra lỗi hệ thống.", ResultErrorType.InternalServerError);
        }
    }
}

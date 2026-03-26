using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetPlacedStudents
{
    public class GetPlacedStudentsHandler : IRequestHandler<GetPlacedStudentsQuery, Result<PaginatedResult<GetPlacedStudentsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetPlacedStudentsHandler> _logger;

        public GetPlacedStudentsHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<GetPlacedStudentsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<GetPlacedStudentsResponse>>> Handle(GetPlacedStudentsQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<PaginatedResult<GetPlacedStudentsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);
            
            if (enterpriseUser == null)
            {
                return Result<PaginatedResult<GetPlacedStudentsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            var query = from app in _unitOfWork.Repository<IOCv2.Domain.Entities.InternshipApplication>().Query().AsNoTracking()
                        where app.EnterpriseId == enterpriseUser.EnterpriseId
                           && app.TermId == request.TermId
                           && app.Status == InternshipApplicationStatus.Placed
                        
                        let assignedGroupMember = _unitOfWork.Repository<InternshipStudent>().Query()
                            .FirstOrDefault(m => m.StudentId == app.StudentId 
                                                 && m.InternshipGroup != null 
                                                 && m.InternshipGroup.EnterpriseId == enterpriseUser.EnterpriseId
                                                 && m.InternshipGroup.TermId == request.TermId
                                                 && m.InternshipGroup.Status == GroupStatus.Active)
                        
                        select new
                        {
                            App = app,
                            Student = app.Student,
                            User = app.Student!.User,
                            AssignedGroup = assignedGroupMember != null ? assignedGroupMember.InternshipGroup : null
                        };

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var lowerSearch = request.SearchTerm.ToLower();
                query = query.Where(q => 
                    q.User!.FullName.ToLower().Contains(lowerSearch) ||
                    q.User.UserCode.ToLower().Contains(lowerSearch) ||
                    (q.User.Email != null && q.User.Email.ToLower().Contains(lowerSearch)));
            }

            if (request.IsAssignedToGroup.HasValue)
            {
                if (request.IsAssignedToGroup.Value)
                    query = query.Where(q => q.AssignedGroup != null);
                else
                    query = query.Where(q => q.AssignedGroup == null);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var pagedData = await query
                .OrderByDescending(q => q.App.AppliedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(q => new GetPlacedStudentsResponse
                {
                    StudentId = q.Student!.StudentId,
                    StudentCode = q.User!.UserCode,
                    FullName = q.User.FullName,
                    Email = q.User.Email ?? string.Empty,
                    Major = q.Student.Major != null ? q.Student.Major : string.Empty,
                    ClassName = q.Student.ClassName != null ? q.Student.ClassName : string.Empty,
                    IsAssignedToGroup = q.AssignedGroup != null,
                    AssignedGroupId = q.AssignedGroup != null ? (Guid?)q.AssignedGroup.InternshipId : null,
                    AssignedGroupName = q.AssignedGroup != null ? q.AssignedGroup.GroupName : null
                })
                .ToListAsync(cancellationToken);

            var paginatedResult = new PaginatedResult<GetPlacedStudentsResponse>(pagedData, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetPlacedStudentsResponse>>.Success(paginatedResult);
        }
    }
}

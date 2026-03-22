using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudents;

public class GetStudentsHandler : IRequestHandler<GetStudentsQuery, Result<PaginatedResult<GetStudentsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetStudentsHandler> _logger;

    public GetStudentsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetStudentsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<GetStudentsResponse>>> Handle(GetStudentsQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        // Find term
        var term = await _unitOfWork.Repository<Term>()
            .Query()
            .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

        if (term == null)
            return Result<PaginatedResult<GetStudentsResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.NotFound);

        // Authorization
        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                return Result<PaginatedResult<GetStudentsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        // Build query
        var query = _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Include(st => st.Enterprise)
            .Where(st => st.TermId == request.TermId);

        // Filters
        if (request.EnrollmentStatus.HasValue)
            query = query.Where(st => st.EnrollmentStatus == request.EnrollmentStatus.Value);

        if (request.PlacementStatus.HasValue)
            query = query.Where(st => st.PlacementStatus == request.PlacementStatus.Value);

        if (!string.IsNullOrWhiteSpace(request.Major))
            query = query.Where(st => st.Student.Major != null && st.Student.Major.Contains(request.Major));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(st =>
                st.Student.User.FullName.ToLower().Contains(search) ||
                st.Student.User.UserCode.ToLower().Contains(search) ||
                st.Student.User.Email.ToLower().Contains(search) ||
                (st.Enterprise != null && st.Enterprise.Name.ToLower().Contains(search)));
        }

        // Sort
        query = request.SortBy.ToLower() switch
        {
            "fullname" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(st => st.Student.User.FullName)
                : query.OrderByDescending(st => st.Student.User.FullName),
            "studentcode" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(st => st.Student.User.UserCode)
                : query.OrderByDescending(st => st.Student.User.UserCode),
            "placementstatus" => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(st => st.PlacementStatus)
                : query.OrderByDescending(st => st.PlacementStatus),
            _ => request.SortOrder.ToLower() == "asc"
                ? query.OrderBy(st => st.EnrollmentDate)
                : query.OrderByDescending(st => st.EnrollmentDate)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(st => new GetStudentsResponse
            {
                StudentTermId = st.StudentTermId,
                StudentId = st.StudentId,
                StudentCode = st.Student.User.UserCode,
                FullName = st.Student.User.FullName,
                Email = st.Student.User.Email,
                Phone = st.Student.User.PhoneNumber,
                Major = st.Student.Major,
                DateOfBirth = st.Student.User.DateOfBirth,
                EnrollmentStatus = st.EnrollmentStatus,
                PlacementStatus = st.PlacementStatus,
                EnrollmentDate = st.EnrollmentDate,
                EnrollmentNote = st.EnrollmentNote,
                EnterpriseId = st.EnterpriseId,
                EnterpriseName = st.Enterprise != null ? st.Enterprise.Name : null
            })
            .ToListAsync(cancellationToken);

        var result = PaginatedResult<GetStudentsResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
        return Result<PaginatedResult<GetStudentsResponse>>.Success(result);
    }
}

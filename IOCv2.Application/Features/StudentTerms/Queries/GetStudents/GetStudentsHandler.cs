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
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetStudentsHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public GetStudentsHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<GetStudentsHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<PaginatedResult<GetStudentsResponse>>> Handle(
        GetStudentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            // Resolve and validate the term
            var term = await _unitOfWork.Repository<Term>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

            if (term == null)
                return Result<PaginatedResult<GetStudentsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermNotFound),
                    ResultErrorType.NotFound);

            // Authorization: SchoolAdmin can only see their own university's terms
            if (!isSuperAdmin)
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query().AsNoTracking()
                    .Where(uu => uu.UserId == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                    return Result<PaginatedResult<GetStudentsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied),
                        ResultErrorType.Forbidden);
            }

            // Build base query
            var query = _unitOfWork.Repository<StudentTerm>()
                .Query()
                .AsNoTracking()
                .Where(st => st.TermId == request.TermId)
                .Include(st => st.Student).ThenInclude(s => s.User)
                .Include(st => st.Enterprise)
                .AsQueryable();

            // Search filter (name / studentCode / email / enterprise name)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term2 = request.SearchTerm.Trim().ToLower();
                query = query.Where(st =>
                    st.Student.User.FullName.ToLower().Contains(term2) ||
                    st.Student.User.UserCode.ToLower().Contains(term2) ||
                    st.Student.User.Email.ToLower().Contains(term2) ||
                    (st.Enterprise != null && st.Enterprise.Name.ToLower().Contains(term2)));
            }

            // Placement status filter
            if (request.PlacementStatus.HasValue)
                query = query.Where(st => st.PlacementStatus == request.PlacementStatus.Value);

            // Enrollment status filter
            if (request.EnrollmentStatus.HasValue)
                query = query.Where(st => st.EnrollmentStatus == request.EnrollmentStatus.Value);

            // Major filter
            if (!string.IsNullOrWhiteSpace(request.Major))
            {
                var major = request.Major.Trim().ToLower();
                query = query.Where(st => st.Student.Major != null && st.Student.Major.ToLower().Contains(major));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            // Sorting
            query = ApplySorting(query, request.SortBy, request.SortOrder);

            // Pagination + projection
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
                    AvatarUrl = st.Student.User.AvatarUrl,
                    DateOfBirth = st.Student.User.DateOfBirth,
                    EnrollmentStatus = st.EnrollmentStatus,
                    PlacementStatus = st.PlacementStatus,
                    EnterpriseId = st.EnterpriseId,
                    EnterpriseName = st.Enterprise != null ? st.Enterprise.Name : null,
                    EnrollmentDate = st.EnrollmentDate
                })
                .ToListAsync(cancellationToken);

            var result = PaginatedResult<GetStudentsResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetStudentsResponse>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }

    private static IQueryable<StudentTerm> ApplySorting(IQueryable<StudentTerm> query, string? sortBy, string? sortOrder)
    {
        var isDescending = !(sortOrder?.ToLower() == "asc"); // default: desc

        return sortBy?.ToLower() switch
        {
            "fullname" => isDescending
                ? query.OrderByDescending(st => st.Student.User.FullName)
                : query.OrderBy(st => st.Student.User.FullName),
            "studentcode" => isDescending
                ? query.OrderByDescending(st => st.Student.User.UserCode)
                : query.OrderBy(st => st.Student.User.UserCode),
            "placementstatus" => isDescending
                ? query.OrderByDescending(st => st.PlacementStatus)
                : query.OrderBy(st => st.PlacementStatus),
            "enrollmentdate" => isDescending
                ? query.OrderByDescending(st => st.EnrollmentDate)
                : query.OrderBy(st => st.EnrollmentDate),
            _ => isDescending
                ? query.OrderByDescending(st => st.EnrollmentDate)
                : query.OrderBy(st => st.EnrollmentDate)
        };
    }
}

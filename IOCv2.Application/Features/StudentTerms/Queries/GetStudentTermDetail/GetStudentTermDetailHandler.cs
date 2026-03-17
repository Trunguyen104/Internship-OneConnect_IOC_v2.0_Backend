using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;

public class GetStudentTermDetailHandler
    : IRequestHandler<GetStudentTermDetailQuery, Result<GetStudentTermDetailResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetStudentTermDetailHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public GetStudentTermDetailHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<GetStudentTermDetailHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<GetStudentTermDetailResponse>> Handle(
        GetStudentTermDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            var studentTerm = await _unitOfWork.Repository<StudentTerm>()
                .Query()
                .AsNoTracking()
                .Include(st => st.Student).ThenInclude(s => s.User)
                .Include(st => st.Term)
                .Include(st => st.Enterprise)
                .FirstOrDefaultAsync(st => st.StudentTermId == request.StudentTermId, cancellationToken);

            if (studentTerm == null)
                return Result<GetStudentTermDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.NotFound),
                    ResultErrorType.NotFound);

            // Authorization
            if (!isSuperAdmin)
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query().AsNoTracking()
                    .Where(uu => uu.UserId == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (universityUser == null || universityUser.UniversityId != studentTerm.Term.UniversityId)
                    return Result<GetStudentTermDetailResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied),
                        ResultErrorType.Forbidden);
            }

            var response = new GetStudentTermDetailResponse
            {
                StudentTermId = studentTerm.StudentTermId,
                TermId = studentTerm.TermId,
                StudentId = studentTerm.StudentId,
                StudentCode = studentTerm.Student.User.UserCode,
                FullName = studentTerm.Student.User.FullName,
                Email = studentTerm.Student.User.Email,
                Phone = studentTerm.Student.User.PhoneNumber,
                DateOfBirth = studentTerm.Student.User.DateOfBirth,
                Major = studentTerm.Student.Major,
                AvatarUrl = studentTerm.Student.User.AvatarUrl,
                EnrollmentDate = studentTerm.EnrollmentDate,
                EnrollmentStatus = studentTerm.EnrollmentStatus,
                EnrollmentNote = studentTerm.EnrollmentNote,
                PlacementStatus = studentTerm.PlacementStatus,
                EnterpriseId = studentTerm.EnterpriseId,
                EnterpriseName = studentTerm.Enterprise?.Name,
                MidtermFeedback = studentTerm.MidtermFeedback,
                FinalFeedback = studentTerm.FinalFeedback,
                CreatedAt = studentTerm.CreatedAt,
                UpdatedAt = studentTerm.UpdatedAt
            };

            return Result<GetStudentTermDetailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }
}

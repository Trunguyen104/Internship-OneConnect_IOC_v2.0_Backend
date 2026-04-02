using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;

public class GetStudentTermDetailHandler : IRequestHandler<GetStudentTermDetailQuery, Result<GetStudentTermDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;

    public GetStudentTermDetailHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
    }

    public async Task<Result<GetStudentTermDetailResponse>> Handle(GetStudentTermDetailQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        var studentTerm = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Include(st => st.Term)
            .Include(st => st.Enterprise)
            .FirstOrDefaultAsync(
                st => st.StudentTermId == request.StudentTermId && st.EnrollmentStatus == EnrollmentStatus.Active,
                cancellationToken);

        if (studentTerm == null)
            return Result<GetStudentTermDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

        // Authorization
        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != studentTerm.Term.UniversityId)
                return Result<GetStudentTermDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        var response = new GetStudentTermDetailResponse
        {
            StudentTermId = studentTerm.StudentTermId,
            StudentId = studentTerm.StudentId,
            StudentCode = studentTerm.Student.User.UserCode,
            FullName = studentTerm.Student.User.FullName,
            Email = studentTerm.Student.User.Email,
            Phone = studentTerm.Student.User.PhoneNumber,
            Major = studentTerm.Student.Major,
            DateOfBirth = studentTerm.Student.User.DateOfBirth,
            TermId = studentTerm.TermId,
            TermName = studentTerm.Term.Name,
            EnrollmentStatus = studentTerm.EnrollmentStatus,
            PlacementStatus = studentTerm.PlacementStatus,
            EnrollmentDate = studentTerm.EnrollmentDate,
            EnrollmentNote = studentTerm.EnrollmentNote,
            EnterpriseId = studentTerm.EnterpriseId,
            EnterpriseName = studentTerm.Enterprise?.Name,
            MidtermFeedback = studentTerm.MidtermFeedback,
            FinalFeedback = studentTerm.FinalFeedback,
            CreatedAt = studentTerm.CreatedAt,
            UpdatedAt = studentTerm.UpdatedAt
        };

        return Result<GetStudentTermDetailResponse>.Success(
            response,
            _messageService.GetMessage(MessageKeys.StudentTerms.GetStudentTermDetailSuccess, request.StudentTermId));
    }
}

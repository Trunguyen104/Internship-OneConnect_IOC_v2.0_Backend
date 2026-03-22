using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;

public class UpdateStudentTermHandler : IRequestHandler<UpdateStudentTermCommand, Result<UpdateStudentTermResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<UpdateStudentTermHandler> _logger;

    public UpdateStudentTermHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<UpdateStudentTermHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<UpdateStudentTermResponse>> Handle(UpdateStudentTermCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        // 1. Load StudentTerm
        var studentTerm = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Include(st => st.Term)
            .FirstOrDefaultAsync(st => st.StudentTermId == request.StudentTermId, cancellationToken);

        if (studentTerm == null)
            return Result<UpdateStudentTermResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

        // 2. Authorization
        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != studentTerm.Term.UniversityId)
                return Result<UpdateStudentTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        // 3. Validate PlacementStatus = Placed requires EnterpriseId
        var targetPlacement = request.PlacementStatus ?? studentTerm.PlacementStatus;
        var targetEnterpriseId = request.EnterpriseId ?? studentTerm.EnterpriseId;

        if (targetPlacement == PlacementStatus.Placed && !targetEnterpriseId.HasValue)
            return Result<UpdateStudentTermResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.EnterpriseIdRequiredWhenPlaced));

        // 4. Validate EnterpriseId exists
        if (request.EnterpriseId.HasValue)
        {
            var enterpriseExists = await _unitOfWork.Repository<Enterprise>()
                .ExistsAsync(e => e.EnterpriseId == request.EnterpriseId.Value, cancellationToken);
            if (!enterpriseExists)
                return Result<UpdateStudentTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.EnterpriseNotFound), ResultErrorType.NotFound);
        }

        // 5. Check email conflict (exclude self)
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != studentTerm.Student.User.Email)
        {
            var emailConflict = await _unitOfWork.Repository<User>()
                .ExistsAsync(u => u.Email == request.Email && u.UserId != studentTerm.Student.UserId, cancellationToken);
            if (emailConflict)
                return Result<UpdateStudentTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.EmailConflict), ResultErrorType.Conflict);
        }

        // 6. Snapshot old state for counter delta
        var wasActive = studentTerm.EnrollmentStatus == EnrollmentStatus.Active;
        var wasUnplaced = studentTerm.PlacementStatus == PlacementStatus.Unplaced;
        var wasPlaced = studentTerm.PlacementStatus == PlacementStatus.Placed;

        // 7. Update User profile
        var user = studentTerm.Student.User;
        var newFullName = request.FullName ?? user.FullName;
        user.UpdateProfile(newFullName, request.Phone ?? user.PhoneNumber, user.AvatarUrl, (IOCv2.Domain.Enums.UserGender?)user.Gender, request.DateOfBirth ?? user.DateOfBirth, null);
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            user.UpdateEmail(request.Email);
        await _unitOfWork.Repository<User>().UpdateAsync(user, cancellationToken);

        // 8. Update Student
        if (!string.IsNullOrWhiteSpace(request.Major))
            studentTerm.Student.Major = request.Major;
        await _unitOfWork.Repository<Student>().UpdateAsync(studentTerm.Student, cancellationToken);

        // 9. Update StudentTerm
        if (request.EnrollmentDate.HasValue)
            studentTerm.EnrollmentDate = request.EnrollmentDate.Value;
        if (request.EnrollmentStatus.HasValue)
            studentTerm.EnrollmentStatus = request.EnrollmentStatus.Value;
        if (request.EnrollmentNote != null)
            studentTerm.EnrollmentNote = request.EnrollmentNote;
        if (request.PlacementStatus.HasValue)
            studentTerm.PlacementStatus = request.PlacementStatus.Value;

        if (studentTerm.PlacementStatus == PlacementStatus.Placed)
            studentTerm.EnterpriseId = request.EnterpriseId ?? studentTerm.EnterpriseId;
        else
        {
            // Unplaced → clear enterprise
            studentTerm.EnterpriseId = null;
        }

        studentTerm.UpdatedBy = userId;
        studentTerm.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<StudentTerm>().UpdateAsync(studentTerm, cancellationToken);

        // 10. Recalculate term counter deltas
        var isNowActive = studentTerm.EnrollmentStatus == EnrollmentStatus.Active;
        var isNowUnplaced = studentTerm.PlacementStatus == PlacementStatus.Unplaced;
        var isNowPlaced = studentTerm.PlacementStatus == PlacementStatus.Placed;

        var term = studentTerm.Term;
        term.TotalEnrolled += (isNowActive ? 1 : 0) - (wasActive ? 1 : 0);
        term.TotalUnplaced += (isNowActive && isNowUnplaced ? 1 : 0) - (wasActive && wasUnplaced ? 1 : 0);
        term.TotalPlaced += (isNowActive && isNowPlaced ? 1 : 0) - (wasActive && wasPlaced ? 1 : 0);
        await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);

        await _unitOfWork.SaveChangeAsync(cancellationToken);

        _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogUpdated), request.StudentTermId, userId);

        return Result<UpdateStudentTermResponse>.Success(
            new UpdateStudentTermResponse { StudentTermId = request.StudentTermId },
            _messageService.GetMessage(MessageKeys.StudentTerms.UpdateSuccess));
    }
}

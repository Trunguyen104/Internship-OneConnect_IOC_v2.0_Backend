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
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateStudentTermHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStudentTermHandler(
        IUnitOfWork unitOfWork, IMessageService messageService,
        ILogger<UpdateStudentTermHandler> logger, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<UpdateStudentTermResponse>> Handle(
        UpdateStudentTermCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
                .Include(st => st.Student).ThenInclude(s => s.User)
                .Include(st => st.Term)
                .FirstOrDefaultAsync(st => st.StudentTermId == request.StudentTermId, cancellationToken);

            if (studentTerm == null)
                return Result<UpdateStudentTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

            // Authorization
            if (!isSuperAdmin)
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
                    .Where(uu => uu.UserId == userId).FirstOrDefaultAsync(cancellationToken);
                if (universityUser == null || universityUser.UniversityId != studentTerm.Term.UniversityId)
                    return Result<UpdateStudentTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied), ResultErrorType.Forbidden);
            }

            // Validate Placed requires Enterprise
            var targetPlacement = request.PlacementStatus ?? studentTerm.PlacementStatus;
            if (targetPlacement == PlacementStatus.Placed && !request.EnterpriseId.HasValue && studentTerm.EnterpriseId == null)
                return Result<UpdateStudentTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.PlacedButNoEnterprise));

            // Validate EnterpriseId exists if provided
            if (request.EnterpriseId.HasValue)
            {
                var enterpriseExists = await _unitOfWork.Repository<Enterprise>().Query().AsNoTracking()
                    .AnyAsync(e => e.EnterpriseId == request.EnterpriseId.Value, cancellationToken);
                if (!enterpriseExists)
                    return Result<UpdateStudentTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.EnterpriseNotFound), ResultErrorType.NotFound);
            }

            // Check email uniqueness if changed (normalize both sides)
            if (!string.IsNullOrWhiteSpace(request.Email) &&
                !request.Email.Trim().Equals(studentTerm.Student.User.Email, StringComparison.OrdinalIgnoreCase))
            {
                var emailTaken = await _unitOfWork.Repository<User>().Query()
                    .AnyAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower() &&
                                   u.UserId != studentTerm.Student.UserId, cancellationToken);
                if (emailTaken)
                    return Result<UpdateStudentTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.EmailExistsInSystem), ResultErrorType.Conflict);
            }

            // Check phone uniqueness if changed (phone has UNIQUE index)
            if (!string.IsNullOrWhiteSpace(request.Phone) &&
                !request.Phone.Trim().Equals(studentTerm.Student.User.PhoneNumber, StringComparison.OrdinalIgnoreCase))
            {
                var phoneTaken = await _unitOfWork.Repository<User>().Query()
                    .AnyAsync(u => u.PhoneNumber == request.Phone.Trim() &&
                                   u.UserId != studentTerm.Student.UserId, cancellationToken);
                if (phoneTaken)
                    return Result<UpdateStudentTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.PhoneExistsInSystem), ResultErrorType.Conflict);
            }

            // Persist email change if provided and passed uniqueness check
            if (!string.IsNullOrWhiteSpace(request.Email))
                studentTerm.Student.User.UpdateEmail(request.Email.Trim().ToLower());

            // Snapshot counter-contribution BEFORE any changes
            var wasActive = studentTerm.EnrollmentStatus == EnrollmentStatus.Active;
            var wasUnplaced = wasActive && studentTerm.PlacementStatus == PlacementStatus.Unplaced;
            var wasPlaced = wasActive && studentTerm.PlacementStatus == PlacementStatus.Placed;

            // Update User profile fields
            studentTerm.Student.User.UpdateProfile(
                request.FullName ?? studentTerm.Student.User.FullName,
                request.Phone ?? studentTerm.Student.User.PhoneNumber,
                null,
                null,
                request.DateOfBirth ?? studentTerm.Student.User.DateOfBirth);

            // Update Student major
            if (request.Major != null) studentTerm.Student.Major = request.Major;

            // Update enrollment fields
            if (request.EnrollmentDate.HasValue) studentTerm.EnrollmentDate = request.EnrollmentDate.Value;
            if (request.EnrollmentStatus.HasValue) studentTerm.EnrollmentStatus = request.EnrollmentStatus.Value;
            if (request.EnrollmentNote != null) studentTerm.EnrollmentNote = request.EnrollmentNote;

            // Update placement fields
            if (request.PlacementStatus.HasValue)
            {
                studentTerm.PlacementStatus = request.PlacementStatus.Value;
                // If switching to Unplaced, always clear enterprise (Bug 5 fix: do this before EnterpriseId assignment)
                if (request.PlacementStatus.Value == PlacementStatus.Unplaced)
                    studentTerm.EnterpriseId = null;
            }
            // Only assign EnterpriseId when the resulting placement is Placed (Bug 5 fix)
            if (request.EnterpriseId.HasValue && studentTerm.PlacementStatus == PlacementStatus.Placed)
                studentTerm.EnterpriseId = request.EnterpriseId.Value;

            // Recalculate counter-deltas based on the new state (Bug 3 fix: covers EnrollmentStatus changes too)
            var isNowActive = studentTerm.EnrollmentStatus == EnrollmentStatus.Active;
            var isNowUnplaced = isNowActive && studentTerm.PlacementStatus == PlacementStatus.Unplaced;
            var isNowPlaced = isNowActive && studentTerm.PlacementStatus == PlacementStatus.Placed;

            var deltaEnrolled = (isNowActive ? 1 : 0) - (wasActive ? 1 : 0);
            var deltaUnplaced = (isNowUnplaced ? 1 : 0) - (wasUnplaced ? 1 : 0);
            var deltaPlaced = (isNowPlaced ? 1 : 0) - (wasPlaced ? 1 : 0);

            if (deltaEnrolled != 0 || deltaUnplaced != 0 || deltaPlaced != 0)
            {
                studentTerm.Term.TotalEnrolled = Math.Max(0, studentTerm.Term.TotalEnrolled + deltaEnrolled);
                studentTerm.Term.TotalUnplaced = Math.Max(0, studentTerm.Term.TotalUnplaced + deltaUnplaced);
                studentTerm.Term.TotalPlaced = Math.Max(0, studentTerm.Term.TotalPlaced + deltaPlaced);
                await _unitOfWork.Repository<Term>().UpdateAsync(studentTerm.Term, cancellationToken);
            }

            await _unitOfWork.Repository<StudentTerm>().UpdateAsync(studentTerm, cancellationToken);
            await _unitOfWork.Repository<User>().UpdateAsync(studentTerm.Student.User, cancellationToken);
            await _unitOfWork.Repository<Student>().UpdateAsync(studentTerm.Student, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogUpdated),
                studentTerm.StudentTermId, userId);

            return Result<UpdateStudentTermResponse>.Success(
                new UpdateStudentTermResponse { StudentTermId = studentTerm.StudentTermId },
                _messageService.GetMessage(MessageKeys.StudentTerms.UpdateSuccess));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }
}

using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.AddStudentManual;

public class AddStudentManualHandler : IRequestHandler<AddStudentManualCommand, Result<AddStudentManualResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AddStudentManualHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IPasswordService _passwordService;
    private readonly IUnitOfWork _unitOfWork;

    public AddStudentManualHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        IPasswordService passwordService,
        ILogger<AddStudentManualHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _passwordService = passwordService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<AddStudentManualResponse>> Handle(
        AddStudentManualCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Resolve term and authorization
            var term = await _unitOfWork.Repository<Term>().Query()
                .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

            if (term == null)
                return Result<AddStudentManualResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermNotFound), ResultErrorType.NotFound);

            // Check if term allows enrollment (only Open terms accept students)
            if (term.Status != TermStatus.Open || term.EndDate < today)
                return Result<AddStudentManualResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermEndedOrClosed));

            Guid universityId;
            if (isSuperAdmin)
            {
                universityId = term.UniversityId;
            }
            else
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
                    .Where(uu => uu.UserId == userId).FirstOrDefaultAsync(cancellationToken);

                if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                    return Result<AddStudentManualResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied), ResultErrorType.Forbidden);

                universityId = universityUser.UniversityId;
            }

            // Check email uniqueness in system
            var emailExists = await _unitOfWork.Repository<User>().Query()
                .AnyAsync(u => u.Email.ToLower() == request.Email.Trim().ToLower(), cancellationToken);
            if (emailExists)
                return Result<AddStudentManualResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.EmailExistsInSystem), ResultErrorType.Conflict);

            // Check studentCode uniqueness within this university (across all terms of this university)
            var codeExists = await _unitOfWork.Repository<StudentTerm>().Query()
                .AnyAsync(st => st.Student.User.UserCode == request.StudentCode.Trim()
                                && st.Term.UniversityId == universityId, cancellationToken);
            if (codeExists)
                return Result<AddStudentManualResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeExistsInUniversity), ResultErrorType.Conflict);

            // Check student not already enrolled in active/upcoming term
            // (Open term whose end date is >= today or start date > today)
            var alreadyInActiveTerm = await _unitOfWork.Repository<StudentTerm>().Query()
                .Include(st => st.Student).ThenInclude(s => s.User)
                .Include(st => st.Term)
                .Where(st =>
                    st.Student.User.UserCode == request.StudentCode.Trim() &&
                    st.Term.UniversityId == universityId &&
                    st.EnrollmentStatus == EnrollmentStatus.Active &&
                    st.Term.Status == TermStatus.Open &&
                    st.Term.EndDate >= today)
                .AnyAsync(cancellationToken);

            if (alreadyInActiveTerm)
                return Result<AddStudentManualResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.AlreadyEnrolledInActiveTerm), ResultErrorType.Conflict);

            // Create User + Student — password is auto-generated; admin distributes it out-of-band
            var temporaryPassword = _passwordService.GenerateRandomPassword();
            var passwordHash = _passwordService.HashPassword(temporaryPassword);
            var newUserId = Guid.NewGuid();
            var newUser = new User(
                newUserId,
                request.StudentCode.Trim(),
                request.Email.Trim().ToLower(),
                request.FullName.Trim(),
                UserRole.Student,
                passwordHash);

            if (request.Phone != null || request.DateOfBirth.HasValue)
                newUser.UpdateProfile(request.FullName.Trim(), request.Phone, null, null, request.DateOfBirth);

            await _unitOfWork.Repository<User>().AddAsync(newUser, cancellationToken);

            var newStudent = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = newUserId,
                Major = request.Major?.Trim(),
                InternshipStatus = StudentStatus.NO_INTERNSHIP
            };
            await _unitOfWork.Repository<Student>().AddAsync(newStudent, cancellationToken);

            // Create enrollment record
            var enrollment = new StudentTerm
            {
                StudentTermId = Guid.NewGuid(),
                TermId = request.TermId,
                StudentId = newStudent.StudentId,
                EnrollmentStatus = EnrollmentStatus.Active,
                PlacementStatus = PlacementStatus.Unplaced,
                EnrollmentDate = today
            };
            await _unitOfWork.Repository<StudentTerm>().AddAsync(enrollment, cancellationToken);

            // Increment term counter
            term.TotalEnrolled++;
            term.TotalUnplaced++;
            await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogAdded),
                enrollment.StudentTermId, userId);

            return Result<AddStudentManualResponse>.Success(
                new AddStudentManualResponse
                {
                    StudentTermId = enrollment.StudentTermId,
                    TemporaryPassword = temporaryPassword
                },
                _messageService.GetMessage(MessageKeys.StudentTerms.AddSuccess));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }
}

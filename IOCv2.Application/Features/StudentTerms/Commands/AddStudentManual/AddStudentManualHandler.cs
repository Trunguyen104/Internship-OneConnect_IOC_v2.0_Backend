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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPasswordService _passwordService;
    private readonly IUserServices _userServices;
    private readonly ILogger<AddStudentManualHandler> _logger;

    public AddStudentManualHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPasswordService passwordService,
        IUserServices userServices,
        ILogger<AddStudentManualHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _passwordService = passwordService;
        _userServices = userServices;
        _logger = logger;
    }

    public async Task<Result<AddStudentManualResponse>> Handle(AddStudentManualCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        // 1. Find term
        var term = await _unitOfWork.Repository<Term>()
            .Query()
            .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

        if (term == null)
            return Result<AddStudentManualResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.NotFound);

        // 2. Term must be Open and not ended
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (term.Status != TermStatus.Open || term.EndDate < today)
            return Result<AddStudentManualResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.TermNotOpen));

        // 3. Authorization
        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                return Result<AddStudentManualResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        // 4. Check email conflict system-wide
        var emailExists = await _unitOfWork.Repository<User>()
            .ExistsAsync(u => u.Email == request.Email, cancellationToken);
        if (emailExists)
            return Result<AddStudentManualResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.EmailConflict), ResultErrorType.Conflict);

        // 5. Check studentCode conflict within same university
        var codeConflict = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Include(st => st.Term)
            .AnyAsync(st =>
                st.Student.User.UserCode == request.StudentCode &&
                st.Term.UniversityId == term.UniversityId,
                cancellationToken);

        if (codeConflict)
            return Result<AddStudentManualResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.StudentCodeConflict), ResultErrorType.Conflict);

        // 6. Check if student already enrolled in another active term
        var alreadyInActiveTerm = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Include(st => st.Term)
            .AnyAsync(st =>
                st.Student.User.Email == request.Email &&
                st.EnrollmentStatus == EnrollmentStatus.Active &&
                st.TermId != request.TermId,
                cancellationToken);

        if (alreadyInActiveTerm)
            return Result<AddStudentManualResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.AlreadyEnrolled), ResultErrorType.Conflict);

        // 7. Create User → Student → StudentTerm
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var tempPassword = _passwordService.GenerateRandomPassword();
            var passwordHash = _passwordService.HashPassword(tempPassword);

            var newUserId = Guid.NewGuid();
            var user = new User(newUserId, request.StudentCode, request.Email, request.FullName, UserRole.Student, passwordHash);
            user.UpdateProfile(request.FullName, request.Phone, null, null, request.DateOfBirth, null);
            await _unitOfWork.Repository<User>().AddAsync(user, cancellationToken);

            var student = new Student
            {
                StudentId = Guid.NewGuid(),
                UserId = newUserId,
                Major = request.Major,
                InternshipStatus = StudentStatus.NO_INTERNSHIP
            };
            await _unitOfWork.Repository<Student>().AddAsync(student, cancellationToken);

            // Also create UniversityUser link
            var universityUserLink = new UniversityUser
            {
                UniversityUserId = Guid.NewGuid(),
                UniversityId = term.UniversityId,
                UserId = newUserId
            };
            await _unitOfWork.Repository<UniversityUser>().AddAsync(universityUserLink, cancellationToken);

            var studentTermId = Guid.NewGuid();
            var studentTerm = new StudentTerm
            {
                StudentTermId = studentTermId,
                TermId = request.TermId,
                StudentId = student.StudentId,
                EnrollmentStatus = EnrollmentStatus.Active,
                PlacementStatus = PlacementStatus.Unplaced,
                EnrollmentDate = today,
                CreatedBy = userId
            };
            await _unitOfWork.Repository<StudentTerm>().AddAsync(studentTerm, cancellationToken);

            // 8. Update term counters
            term.TotalEnrolled++;
            term.TotalUnplaced++;
            await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogAdded), studentTermId, userId);

            return Result<AddStudentManualResponse>.Success(
                new AddStudentManualResponse { StudentTermId = studentTermId, TemporaryPassword = tempPassword },
                _messageService.GetMessage(MessageKeys.StudentTerms.AddSuccess));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error adding student manually to term {TermId}", request.TermId);
            throw;
        }
    }
}

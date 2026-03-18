using ClosedXML.Excel;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudents;

public class ImportStudentsConfirmHandler
    : IRequestHandler<ImportStudentsConfirmCommand, Result<ImportStudentsConfirmResponse>>
{
    private readonly IBackgroundEmailSender _emailSender;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ImportStudentsConfirmHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IPasswordService _passwordService;
    private readonly IUnitOfWork _unitOfWork;

    public ImportStudentsConfirmHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        IPasswordService passwordService,
        ILogger<ImportStudentsConfirmHandler> logger,
        ICurrentUserService currentUserService,
        IBackgroundEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _passwordService = passwordService;
        _logger = logger;
        _currentUserService = currentUserService;
        _emailSender = emailSender;
    }

    public async Task<Result<ImportStudentsConfirmResponse>> Handle(
        ImportStudentsConfirmCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (request.ValidRecords == null || request.ValidRecords.Count == 0)
                return Result<ImportStudentsConfirmResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.FileEmpty));

            // Validate term
            var term = await _unitOfWork.Repository<Term>().Query()
                .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

            if (term == null)
                return Result<ImportStudentsConfirmResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermNotFound), ResultErrorType.NotFound);

            // Only Open terms accept new enrollments
            if (term.Status != TermStatus.Open || term.EndDate < today)
                return Result<ImportStudentsConfirmResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermEndedOrClosed));

            if (!isSuperAdmin)
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
                    .Where(uu => uu.UserId == userId).FirstOrDefaultAsync(cancellationToken);
                if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                    return Result<ImportStudentsConfirmResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied), ResultErrorType.Forbidden);
            }

            // Results tracking
            var passwordEntries = new List<(string Code, string Name, string Email, string Password)>();
            int imported = 0, skipped = 0;

            // Load existing enrolled codes AND emails for this term
            var existingEnrolments = await _unitOfWork.Repository<StudentTerm>().Query().AsNoTracking()
                .Where(st => st.TermId == request.TermId && st.EnrollmentStatus == EnrollmentStatus.Active)
                .Select(st => new { Code = st.Student.User.UserCode, Email = st.Student.User.Email })
                .ToListAsync(cancellationToken);
            var existingCodes = existingEnrolments.Select(e => e.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingEmails = existingEnrolments.Select(e => e.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var record in request.ValidRecords)
            {
                // Skip if already enrolled by code or email
                if (existingCodes.Contains(record.StudentCode) || existingEmails.Contains(record.Email.ToLower()))
                { skipped++; continue; }

                // Check if a user already exists in a system
                var existingUser = await _unitOfWork.Repository<User>().Query()
                    .Include(u => u.Student)
                    .FirstOrDefaultAsync(u => u.UserCode == record.StudentCode || 
                        u.Email.ToLower() == record.Email.ToLower(), cancellationToken);

                Guid studentId;
                string plainPassword = string.Empty;

                if (existingUser != null && existingUser.Student == null)
                {
                    // User exists but is not a Student (e.g., SchoolAdmin) — skip to avoid a duplicate key crash
                    skipped++;
                    continue;
                }

                // Check phone uniqueness if provided (phone has UNIQUE index)
                if (!string.IsNullOrWhiteSpace(record.Phone) && existingUser == null)
                {
                    var phoneTaken = await _unitOfWork.Repository<User>().Query()
                        .AnyAsync(u => u.PhoneNumber == record.Phone.Trim(), cancellationToken);
                    if (phoneTaken) { skipped++; continue; }
                }

                if (existingUser?.Student != null)
                {
                    // Use existing student record
                    studentId = existingUser.Student.StudentId;
                }
                else
                {
                    // Create a new User + Student
                    plainPassword = _passwordService.GenerateRandomPassword();
                    var passwordHash = _passwordService.HashPassword(plainPassword);
                    var newUserId = Guid.NewGuid();
                    var newUser = new User(newUserId, record.StudentCode, record.Email.ToLower(),
                        record.FullName, UserRole.Student, passwordHash);

                    DateOnly? dob = null;
                    if (!string.IsNullOrWhiteSpace(record.DateOfBirth))
                    {
                        if (DateOnly.TryParseExact(record.DateOfBirth,
                            new[] { "dd/MM/yyyy", "yyyy-MM-dd" }, null,
                            System.Globalization.DateTimeStyles.None, out var parsedDob))
                            dob = parsedDob;
                    }

                    if (!string.IsNullOrWhiteSpace(record.Phone) || dob.HasValue)
                        newUser.UpdateProfile(record.FullName, record.Phone, null, null, dob);

                    await _unitOfWork.Repository<User>().AddAsync(newUser, cancellationToken);

                    var newStudent = new Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = newUserId,
                        Major = record.Major?.Trim(),
                        InternshipStatus = StudentStatus.NO_INTERNSHIP
                    };
                    await _unitOfWork.Repository<Student>().AddAsync(newStudent, cancellationToken);
                    studentId = newStudent.StudentId;

                    passwordEntries.Add((record.StudentCode, record.FullName, record.Email, plainPassword));
                }

                // Bug E: cross-term race condition check — a student may have been enrolled in another active term
                // between Preview and Confirm steps
                var inOtherActiveTerm = await _unitOfWork.Repository<StudentTerm>().Query()
                    .AnyAsync(st =>
                        st.StudentId == studentId &&
                        st.EnrollmentStatus == EnrollmentStatus.Active &&
                        st.Term.Status == TermStatus.Open &&
                        st.Term.EndDate >= today &&
                        st.TermId != request.TermId, cancellationToken);
                if (inOtherActiveTerm) { skipped++; continue; }

                // Bug A: check for existing record in this term (covers re-import of a Withdrawn student)
                var existingRecord = await _unitOfWork.Repository<StudentTerm>().Query()
                    .FirstOrDefaultAsync(st => st.StudentId == studentId && st.TermId == request.TermId, cancellationToken);

                if (existingRecord != null)
                {
                    if (existingRecord.EnrollmentStatus == EnrollmentStatus.Active)
                    { skipped++; continue; }

                    // Re-activate a withdrawn record instead of inserting a duplicate
                    existingRecord.EnrollmentStatus = EnrollmentStatus.Active;
                    existingRecord.PlacementStatus = PlacementStatus.Unplaced;
                    existingRecord.EnterpriseId = null;
                    existingRecord.EnrollmentDate = today;
                    await _unitOfWork.Repository<StudentTerm>().UpdateAsync(existingRecord, cancellationToken);
                }
                else
                {
                    var enrollment = new StudentTerm
                    {
                        StudentTermId = Guid.NewGuid(),
                        TermId = request.TermId,
                        StudentId = studentId,
                        EnrollmentStatus = EnrollmentStatus.Active,
                        PlacementStatus = PlacementStatus.Unplaced,
                        EnrollmentDate = today
                    };
                    await _unitOfWork.Repository<StudentTerm>().AddAsync(enrollment, cancellationToken);
                }

                existingCodes.Add(record.StudentCode);   // prevent same-code double-add in batch
                existingEmails.Add(record.Email.ToLower()); // prevent same-email double-add in batch
                imported++;
            }

            // Update term counters
            term.TotalEnrolled += imported;
            term.TotalUnplaced += imported;
            await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Send account creation emails to new students (fire-and-forget via background channel)
            foreach (var entry in passwordEntries)
            {
                await _emailSender.EnqueueAccountCreationEmailAsync(
                    entry.Email, entry.Name, entry.Email, "Student", entry.Password,
                    cancellationToken: cancellationToken);
            }

            // Generate a password Excel file in memory
            byte[]? fileContent = null;
            string? fileName = null;
            if (passwordEntries.Count > 0)
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Mật khẩu sinh viên");
                ws.Cell(1, 1).Value = "MSSV";
                ws.Cell(1, 2).Value = "Họ và tên";
                ws.Cell(1, 3).Value = "Email";
                ws.Cell(1, 4).Value = "Mật khẩu tạm";

                for (var i = 0; i < passwordEntries.Count; i++)
                {
                    ws.Cell(i + 2, 1).Value = passwordEntries[i].Code;
                    ws.Cell(i + 2, 2).Value = passwordEntries[i].Name;
                    ws.Cell(i + 2, 3).Value = passwordEntries[i].Email;
                    ws.Cell(i + 2, 4).Value = passwordEntries[i].Password;
                }

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                fileContent = ms.ToArray();
                fileName = $"student_passwords_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            }

            var message = skipped > 0
                ? string.Format(_messageService.GetMessage(MessageKeys.StudentTerms.ImportPartialSuccess), imported, skipped)
                : string.Format(_messageService.GetMessage(MessageKeys.StudentTerms.ImportConfirmSuccess), imported);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogImported), imported, userId);

            return Result<ImportStudentsConfirmResponse>.Success(
                new ImportStudentsConfirmResponse
                {
                    ImportedCount = imported,
                    SkippedCount = skipped,
                    PasswordFileContent = fileContent,
                    PasswordFileFileName = fileName
                }, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }
}

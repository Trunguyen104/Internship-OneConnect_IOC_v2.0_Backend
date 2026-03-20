using ClosedXML.Excel;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsConfirm;

public class ImportStudentsConfirmHandler : IRequestHandler<ImportStudentsConfirmCommand, Result<ImportStudentsConfirmResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPasswordService _passwordService;
    private readonly ILogger<ImportStudentsConfirmHandler> _logger;

    public ImportStudentsConfirmHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPasswordService passwordService,
        ILogger<ImportStudentsConfirmHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task<Result<ImportStudentsConfirmResponse>> Handle(ImportStudentsConfirmCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        var term = await _unitOfWork.Repository<Term>()
            .Query()
            .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

        if (term == null)
            return Result<ImportStudentsConfirmResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.NotFound);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (term.Status != TermStatus.Open || term.EndDate < today)
            return Result<ImportStudentsConfirmResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.TermNotOpen));

        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                return Result<ImportStudentsConfirmResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        // Load existing enrollments in this term
        var existingInTerm = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Where(st => st.TermId == request.TermId)
            .ToListAsync(cancellationToken);

        var existingCodesInTerm = existingInTerm
            .Where(st => st.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(st => st.Student.User.UserCode.ToLower())
            .ToHashSet();

        var existingEmailsInTerm = existingInTerm
            .Where(st => st.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(st => st.Student.User.Email.ToLower())
            .ToHashSet();

        // Load cross-term active enrollments
        var crossTermActive = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Where(st => st.TermId != request.TermId && st.EnrollmentStatus == EnrollmentStatus.Active)
            .Select(st => new { Code = st.Student.User.UserCode.ToLower(), Email = st.Student.User.Email.ToLower() })
            .ToListAsync(cancellationToken);

        var crossTermCodes = crossTermActive.Select(x => x.Code).ToHashSet();
        var crossTermEmails = crossTermActive.Select(x => x.Email).ToHashSet();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            int imported = 0;
            int skipped = 0;
            var newPasswordEntries = new List<(string Code, string FullName, string Email, string Password)>();

            foreach (var record in request.ValidRecords)
            {
                var codeLower = record.StudentCode.ToLower();
                var emailLower = record.Email.ToLower();

                // Skip if already active in this term
                if (existingCodesInTerm.Contains(codeLower) || existingEmailsInTerm.Contains(emailLower))
                {
                    skipped++;
                    continue;
                }

                // Skip if enrolled in cross-term
                if (crossTermCodes.Contains(codeLower) || crossTermEmails.Contains(emailLower))
                {
                    skipped++;
                    continue;
                }

                // Find or create User + Student
                var existingUser = await _unitOfWork.Repository<User>()
                    .Query()
                    .FirstOrDefaultAsync(u => u.Email == record.Email, cancellationToken);

                Guid studentId;
                string? tempPassword = null;

                if (existingUser != null)
                {
                    // User exists - check if Student
                    var existingStudent = await _unitOfWork.Repository<Student>()
                        .Query()
                        .FirstOrDefaultAsync(s => s.UserId == existingUser.UserId, cancellationToken);

                    if (existingStudent == null)
                    {
                        // User exists but not a student - skip
                        skipped++;
                        continue;
                    }

                    studentId = existingStudent.StudentId;
                }
                else
                {
                    // Create new User + Student
                    tempPassword = _passwordService.GenerateRandomPassword();
                    var passwordHash = _passwordService.HashPassword(tempPassword);
                    var newUserId = Guid.NewGuid();

                    var newUser = new User(newUserId, record.StudentCode, record.Email, record.FullName, UserRole.Student, passwordHash);
                    newUser.UpdateProfile(record.FullName, record.Phone, null, null, ParseDob(record.DateOfBirth));
                    await _unitOfWork.Repository<User>().AddAsync(newUser, cancellationToken);

                    var universityUserLink = new UniversityUser
                    {
                        UniversityUserId = Guid.NewGuid(),
                        UniversityId = term.UniversityId,
                        UserId = newUserId
                    };
                    await _unitOfWork.Repository<UniversityUser>().AddAsync(universityUserLink, cancellationToken);

                    var newStudent = new Student
                    {
                        StudentId = Guid.NewGuid(),
                        UserId = newUserId,
                        Major = record.Major,
                        InternshipStatus = StudentStatus.NO_INTERNSHIP
                    };
                    await _unitOfWork.Repository<Student>().AddAsync(newStudent, cancellationToken);

                    studentId = newStudent.StudentId;
                    newPasswordEntries.Add((record.StudentCode, record.FullName, record.Email, tempPassword));
                }

                // Check if Withdrawn record exists → re-activate
                var withdrawnRecord = existingInTerm
                    .FirstOrDefault(st => st.StudentId == studentId && st.EnrollmentStatus == EnrollmentStatus.Withdrawn);

                if (withdrawnRecord != null)
                {
                    withdrawnRecord.EnrollmentStatus = EnrollmentStatus.Active;
                    withdrawnRecord.PlacementStatus = PlacementStatus.Unplaced;
                    withdrawnRecord.EnterpriseId = null;
                    withdrawnRecord.EnrollmentDate = today;
                    withdrawnRecord.UpdatedBy = userId;
                    withdrawnRecord.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.Repository<StudentTerm>().UpdateAsync(withdrawnRecord, cancellationToken);
                }
                else
                {
                    var newStudentTerm = new StudentTerm
                    {
                        StudentTermId = Guid.NewGuid(),
                        TermId = request.TermId,
                        StudentId = studentId,
                        EnrollmentStatus = EnrollmentStatus.Active,
                        PlacementStatus = PlacementStatus.Unplaced,
                        EnrollmentDate = today,
                        CreatedBy = userId
                    };
                    await _unitOfWork.Repository<StudentTerm>().AddAsync(newStudentTerm, cancellationToken);
                }

                imported++;
            }

            // Update term counters
            term.TotalEnrolled += imported;
            term.TotalUnplaced += imported;
            await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogImportConfirmed), imported, request.TermId, userId);

            // Generate password Excel file
            string? passwordFileBase64 = null;
            string? passwordFileName = null;

            if (newPasswordEntries.Count > 0)
            {
                using var wb = new XLWorkbook();
                var ws = wb.Worksheets.Add("Mật khẩu tạm");
                ws.Cell(1, 1).Value = "Mã sinh viên";
                ws.Cell(1, 2).Value = "Họ và tên";
                ws.Cell(1, 3).Value = "Email";
                ws.Cell(1, 4).Value = "Mật khẩu tạm";
                ws.Row(1).Style.Font.Bold = true;

                for (int i = 0; i < newPasswordEntries.Count; i++)
                {
                    var (code, fullName, email, pwd) = newPasswordEntries[i];
                    ws.Cell(i + 2, 1).Value = code;
                    ws.Cell(i + 2, 2).Value = fullName;
                    ws.Cell(i + 2, 3).Value = email;
                    ws.Cell(i + 2, 4).Value = pwd;
                }
                ws.Columns().AdjustToContents();

                using var ms = new MemoryStream();
                wb.SaveAs(ms);
                passwordFileBase64 = Convert.ToBase64String(ms.ToArray());
                passwordFileName = $"student_passwords_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
            }

            return Result<ImportStudentsConfirmResponse>.Success(
                new ImportStudentsConfirmResponse
                {
                    ImportedCount = imported,
                    SkippedCount = skipped,
                    PasswordFileBase64 = passwordFileBase64,
                    PasswordFileFileName = passwordFileName
                },
                _messageService.GetMessage(MessageKeys.StudentTerms.ImportConfirmSuccess));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error confirming import for term {TermId}", request.TermId);
            throw;
        }
    }

    private static DateOnly? ParseDob(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return DateOnly.TryParseExact(value, "dd/MM/yyyy", out var result) ? result : null;
    }
}

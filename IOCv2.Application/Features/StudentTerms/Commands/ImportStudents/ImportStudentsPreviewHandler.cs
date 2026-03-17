using System.Text.RegularExpressions;
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

public class ImportStudentsPreviewHandler
    : IRequestHandler<ImportStudentsPreviewCommand, Result<ImportStudentsPreviewResponse>>
{
    private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
    private const int MaxRows = 1000;
    private static readonly string[] AllowedExtensions = { ".xlsx", ".xls" };
    private static readonly string[] RequiredColumns =
        { "Mã sinh viên", "Họ và tên", "Email", "Số điện thoại", "Ngày sinh" };

    private static readonly Regex VietnameseNameRegex =
        new(@"^[\p{L}\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex =
        new(@"^(\+84|0)[0-9]{9,10}$", RegexOptions.Compiled);
    private static readonly Regex StudentCodeRegex =
        new(@"^[a-zA-Z0-9\-_\.]+$", RegexOptions.Compiled);

    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<ImportStudentsPreviewHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public ImportStudentsPreviewHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<ImportStudentsPreviewHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ImportStudentsPreviewResponse>> Handle(
        ImportStudentsPreviewCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Validate term
            var term = await _unitOfWork.Repository<Term>().Query().AsNoTracking()
                .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

            if (term == null)
                return Result<ImportStudentsPreviewResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermNotFound), ResultErrorType.NotFound);

            // Only Open terms accept new enrollments
            if (term.Status != TermStatus.Open || term.EndDate < today)
                return Result<ImportStudentsPreviewResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermEndedOrClosed));

            if (!isSuperAdmin)
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query().AsNoTracking()
                    .Where(uu => uu.UserId == userId).FirstOrDefaultAsync(cancellationToken);
                if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                    return Result<ImportStudentsPreviewResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied), ResultErrorType.Forbidden);
            }

            // Validate file format
            var ext = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                return Result<ImportStudentsPreviewResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.FileInvalidFormat));

            // Validate file size
            if (request.File.Length > MaxFileSizeBytes)
                return Result<ImportStudentsPreviewResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.FileSizeExceeded));

            // Parse Excel
            await using var stream = request.File.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.First();
            var headerRow = sheet.Row(1);

            // Validate template structure
            var headers = Enumerable.Range(1, 5)
                .Select(i => headerRow.Cell(i).GetString().Trim())
                .ToArray();
            for (var i = 0; i < RequiredColumns.Length; i++)
            {
                if (!headers[i].Equals(RequiredColumns[i], StringComparison.OrdinalIgnoreCase))
                    return Result<ImportStudentsPreviewResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.FileInvalidTemplate));
            }

            // Read data rows (skip header, skip empty rows)
            var dataRows = sheet.RowsUsed()
                .Skip(1)
                .Where(r => !r.IsEmpty())
                .ToList();

            if (dataRows.Count == 0)
                return Result<ImportStudentsPreviewResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.FileEmpty));

            if (dataRows.Count > MaxRows)
                return Result<ImportStudentsPreviewResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.FileRowLimitExceeded));

            // Load existing enrollments for this term (to detect duplicates)
            var existingEnrolments = (await _unitOfWork.Repository<StudentTerm>().Query().AsNoTracking()
                .Where(st => st.TermId == request.TermId && st.EnrollmentStatus == EnrollmentStatus.Active)
                .Select(st => new { Code = st.Student.User.UserCode, Email = st.Student.User.Email })
                .ToListAsync(cancellationToken));
            var existingEnrolledCodes = existingEnrolments.Select(e => e.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var existingEnrolledEmails = existingEnrolments.Select(e => e.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Load codes AND emails enrolled in OTHER active terms (cross-term conflict check — Bug 9 fix)
            var activeTermEnrolments = (await _unitOfWork.Repository<StudentTerm>().Query().AsNoTracking()
                .Where(st =>
                    st.EnrollmentStatus == EnrollmentStatus.Active &&
                    st.Term.Status == TermStatus.Open &&
                    st.Term.EndDate >= today &&
                    st.TermId != request.TermId)
                .Select(st => new { Code = st.Student.User.UserCode, Email = st.Student.User.Email })
                .ToListAsync(cancellationToken));
            var activeTermEnrolledCodes = activeTermEnrolments.Select(e => e.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var activeTermEnrolledEmails = activeTermEnrolments.Select(e => e.Email).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var previewRows = new List<ImportPreviewRow>();

            foreach (var row in dataRows)
            {
                var rowNum = row.RowNumber();
                var studentCode = row.Cell(1).GetString().Trim();
                var fullName = row.Cell(2).GetString().Trim();
                var email = row.Cell(3).GetString().Trim();
                var phone = row.Cell(4).GetString().Trim();
                var dob = row.Cell(5).GetString().Trim();
                var errors = new List<string>();

                // Validate each field
                if (string.IsNullOrWhiteSpace(studentCode) || !StudentCodeRegex.IsMatch(studentCode))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.RowStudentCodeInvalid));

                if (string.IsNullOrWhiteSpace(fullName) || !VietnameseNameRegex.IsMatch(fullName))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.RowNameInvalid));

                if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.RowEmailInvalid));

                if (!string.IsNullOrWhiteSpace(phone) && !PhoneRegex.IsMatch(phone))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.RowPhoneInvalid));

                if (!string.IsNullOrWhiteSpace(dob) && !IsValidDob(dob))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.RowDobInvalid));

                // In-file duplicates
                if (!string.IsNullOrWhiteSpace(studentCode) && !seenCodes.Add(studentCode))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.RowStudentCodeDuplicate));

                if (!string.IsNullOrWhiteSpace(email) && !seenEmails.Add(email))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.RowEmailDuplicate));

                // Already enrolled in this term (by code or email)
                if ((!string.IsNullOrWhiteSpace(studentCode) && existingEnrolledCodes.Contains(studentCode)) ||
                    (!string.IsNullOrWhiteSpace(email) && existingEnrolledEmails.Contains(email)))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.RowAlreadyEnrolled));

                // Already enrolled in another active term (by code or email — Bug 9 fix)
                if ((!string.IsNullOrWhiteSpace(studentCode) && activeTermEnrolledCodes.Contains(studentCode)) ||
                    (!string.IsNullOrWhiteSpace(email) && activeTermEnrolledEmails.Contains(email)))
                    errors.Add(_messageService.GetMessage(MessageKeys.StudentTerms.AlreadyEnrolledInActiveTerm));

                previewRows.Add(new ImportPreviewRow
                {
                    RowNumber = rowNum,
                    StudentCode = studentCode,
                    FullName = fullName,
                    Email = email,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone,
                    DateOfBirth = string.IsNullOrWhiteSpace(dob) ? null : dob,
                    IsValid = errors.Count == 0,
                    Errors = errors
                });
            }

            var response = new ImportStudentsPreviewResponse
            {
                TotalRows = previewRows.Count,
                ValidRows = previewRows.Count(r => r.IsValid),
                InvalidRows = previewRows.Count(r => !r.IsValid),
                PreviewData = previewRows
            };

            return Result<ImportStudentsPreviewResponse>.Success(response,
                _messageService.GetMessage(MessageKeys.StudentTerms.ImportPreviewSuccess));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }

    private static bool IsValidEmail(string email)
    {
        try { var _ = new System.Net.Mail.MailAddress(email); return true; }
        catch { return false; }
    }

    private static bool IsValidDob(string dob)
    {
        // Try dd/MM/yyyy or yyyy-MM-dd
        if (DateOnly.TryParseExact(dob, new[] { "dd/MM/yyyy", "yyyy-MM-dd" }, null,
                System.Globalization.DateTimeStyles.None, out var date))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var age = today.Year - date.Year;
            if (date.AddYears(age) > today) age--;
            return age >= 15;
        }
        return false;
    }
}
